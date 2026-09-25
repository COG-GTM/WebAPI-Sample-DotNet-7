# .NET 7 baseline (pre-migration known-good state)

Recorded 2026-09-25 against `main` @ `4b154b0` (Merge pull request #14, GraphQL spec).
Purpose: a reference point so that any regression introduced by the .NET 7 → .NET 10
migration steps on `upgrade/net10` can be attributed. No code was changed to produce this.

## 1. Build and test

| Item | Value |
|---|---|
| SDK used | .NET SDK **7.0.410** (linux-x64, installed via `dotnet-install.sh --channel 7.0`) |
| `dotnet restore Sample.sln` | OK |
| `dotnet build Sample.sln --no-restore` | **Build succeeded — 3 warnings, 0 errors** |
| `dotnet test Sample.sln --no-build` | **20 passed / 0 failed / 0 skipped** |

Warnings (all nullable-reference analysis, none from the SDK/package layer):

| Code | Count | Location |
|---|---|---|
| CS8604 | 1 | `src/WebApi/Program.cs(16,46)` — possible null `connectionString` passed to `AddNpgSql` |
| CS8625 | 2 | `tests/WebApi.Tests/EducationsControllerTests.cs(107,48)` and `(170,63)` — null literal to non-nullable type |

Test counts per project:

| Project | Total | Passed | Failed | Skipped |
|---|---|---|---|---|
| `tests/Application.Tests` | 6 | 6 | 0 | 0 |
| `tests/WebApi.Tests` | 14 | 14 | 0 | 0 |
| **Total** | **20** | **20** | 0 | 0 |

Package baseline (from the csproj files, all net7.0): EF Core 7.0.x + `Npgsql.EntityFrameworkCore.PostgreSQL`
(NodaTime plugin), `Mapster`, `Swashbuckle.AspNetCore`, `AspNetCore.HealthChecks.NpgSql`,
`xunit` + `FakeItEasy` in the test projects.

## 2. docker-compose run and REST smoke test

Command: `docker compose up -d --build` (services `webapi` + `postgres_db`; `pgadmin` was not
started because anonymous Docker Hub pulls were rate-limited during the run — the `postgres`
image was pulled from `mirror.gcr.io/library/postgres` and tagged `postgres:latest`).

### EF migration `20240113141226_Initialize`

- Migrations are **not** applied automatically: `src/WebApi/Program.cs` has no `Migrate()` call
  (the README also says to run `update-database` by hand). Straight after `compose up` the API
  returned HTTP 500 with Npgsql `3D000: database "SampleDB" does not exist`.
- Applied manually with dotnet-ef 7.0.20:
  `dotnet ef database update --project src/Infrastructure --startup-project src/WebApi`
  (connection string pointed at `localhost:5433`).
- Verified in postgres:

  ```
  $ docker exec postgres_container psql -U sara -d SampleDB -c 'select * from "__EFMigrationsHistory";'
        MigrationId          | ProductVersion
  -----------------------------+----------------
   20240113141226_Initialize   | 7.0.10
  ```

- The compose connection string contains `IntegratedSecurity=true`; Npgsql 7 accepted it without
  error (worth re-checking after the Npgsql upgrade, as unknown keywords may be rejected).

### curl transcript (abridged; status lines are exact)

```
$ curl -s -i http://localhost:5000/health
HTTP/1.1 404 Not Found

$ curl -s -i http://localhost:5000/api/educations
HTTP/1.1 200 OK
[{"id":"c92ea179-dd5c-46ca-b7b5-b44a191b974c","degree":"Bachelor's degree","fieldOfStudy":"Software engineering","school":"Sample university","description":null}]

$ curl -s -i -X POST http://localhost:5000/api/educations -H "Content-Type: application/json" \
    -d '{"degree":"Masters degree","fieldOfStudy":"Computer Science","school":"Baseline University","description":"smoke test"}'
HTTP/1.1 200 OK
{"id":"e409004c-bb7d-40c5-98c4-db74141625e8","degree":"Masters degree","fieldOfStudy":"Computer Science","school":"Baseline University","description":"smoke test"}

$ curl -s -i http://localhost:5000/api/educations/e409004c-bb7d-40c5-98c4-db74141625e8
HTTP/1.1 200 OK
{"id":"e409004c-bb7d-40c5-98c4-db74141625e8","degree":"Masters degree",...}

# PUT without "id" in the body -> 400 (EducationService.Update requires body id == route id)
$ curl -s -i -X PUT http://localhost:5000/api/educations/e409004c-... -H "Content-Type: application/json" \
    -d '{"degree":"PhD","fieldOfStudy":"Computer Science","school":"Baseline University","description":"updated by smoke"}'
HTTP/1.1 400 Bad Request
{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"Bad Request","status":400,"traceId":"..."}

# PUT with matching "id" in the body -> 200
$ curl -s -i -X PUT http://localhost:5000/api/educations/e409004c-... -H "Content-Type: application/json" \
    -d '{"id":"e409004c-bb7d-40c5-98c4-db74141625e8","degree":"PhD","fieldOfStudy":"Computer Science","school":"Baseline University","description":"updated by smoke"}'
HTTP/1.1 200 OK

$ curl -s -i http://localhost:5000/api/educations/e409004c-bb7d-40c5-98c4-db74141625e8
HTTP/1.1 200 OK
{"id":"e409004c-bb7d-40c5-98c4-db74141625e8","degree":"PhD","fieldOfStudy":"Computer Science","school":"Baseline University","description":"updated by smoke"}

$ curl -s -i -X DELETE http://localhost:5000/api/educations/e409004c-bb7d-40c5-98c4-db74141625e8
HTTP/1.1 200 OK

$ curl -s -i http://localhost:5000/api/educations/e409004c-bb7d-40c5-98c4-db74141625e8
HTTP/1.1 204 No Content
```

### Behaviours to preserve (or knowingly change) during the migration

These are baseline facts, not bugs to fix in the migration tickets:

- `GET /health` → **404**. `AddHealthChecks().AddNpgSql(...)` is registered but `MapHealthChecks`
  is never called, so there is no health endpoint.
- `GET /api/educations/{id}` for a missing entity → **204 No Content**, not 404.
- `PUT /api/educations/{id}` requires `id` in the JSON body equal to the route id, otherwise 400.
- POST / PUT / DELETE all return **200** (no 201/204).
- EF migrations must be applied manually; the API does not migrate at startup.

## 3. CI status (`.github/workflows/dotnet.yml`)

- The workflow pins `actions/setup-dotnet@v3` with `dotnet-version: 6.0.x`, while every project
  targets `net7.0`.
- The most recent run on `main` is **failing**: run
  [21703573771](https://github.com/COG-GTM/WebAPI-Sample-DotNet-7/actions/runs/21703573771)
  (push of `4b154b0`, 2026-02-05, conclusion `failure`, job `build` failed). The step logs have
  expired (GitHub returns HTTP 410), so the exact error is not retrievable; the expected failure
  with a 6.0.x SDK building net7.0 projects is `NETSDK1045` ("The current .NET SDK does not
  support targeting .NET 7.0"). Recent green runs exist only on PR branches that also change the
  workflow's SDK version.
- Conclusion: **CI on `main` is not a usable baseline.** The migration must fix the workflow SDK
  pin (to 10.0.x) as part of the upgrade; "CI green" for later tickets means green on the PR
  branch against `upgrade/net10`, not parity with `main`.

## Reproduce

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 7.0 --install-dir ~/.dotnet7
export PATH=~/.dotnet7:$PATH DOTNET_ROOT=~/.dotnet7
dotnet restore Sample.sln
dotnet build Sample.sln --no-restore
dotnet test Sample.sln --no-build --verbosity normal
docker compose up -d --build webapi postgres_db
dotnet tool install -g dotnet-ef --version 7.0.20
dotnet ef database update --project src/Infrastructure --startup-project src/WebApi \
  --connection "User ID=sara;Password=mysecretpassword;Server=localhost;Port=5433;Database=SampleDB;Pooling=true;"
```
