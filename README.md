## Java 21 / Spring Boot Web API Sample

Educations REST API, migrated from the original ASP.NET Core (.NET 7) sample to **Java 21 + Spring Boot 4**. The HTTP contract (routes, status codes, media types, JSON shape and the PostgreSQL schema) is unchanged; see [Migration notes](#migration-notes).

## Features & Technologies
- Java 21, Spring Boot 4 (Spring MVC, Spring Data JPA / Hibernate, Bean Validation)
- PostgreSQL with Flyway migrations
- OpenAPI 3.1 contract in [`api-spec/openapi.yaml`](api-spec/openapi.yaml), Swagger UI via springdoc
- Global error handling (`@RestControllerAdvice`) with RFC 7807 `application/problem+json` responses
- Actuator health/readiness probes and Prometheus metrics
- JUnit 5, Mockito, MockMvc, Testcontainers
- Docker / docker compose

## Project layout

```
api-spec/openapi.yaml                 OpenAPI 3.1 contract (source of truth)
src/main/java/com/sample/webapi/
  domain/        Education entity + Spring Data repository
  application/   EducationDto, EducationService, mapper
  web/           EducationsController, ApiExceptionHandler, problem responses
  config/        Case-insensitive routing, OpenAPI metadata
src/main/resources/
  application.yml, application-dev.yml
  db/migration/V1__initialize.sql     Schema + seed row (same as the former EF Core migration)
src/test/java/...                     Unit, web-layer, integration and OpenAPI contract tests
parity/                               Black-box API parity harness (.NET baseline vs Java)
```

## Endpoints

| Method | Path | Result |
|---|---|---|
| GET | `/api/Educations` | 200 list |
| GET | `/api/Educations/{id}` | 200 record, 204 if not found, 404 if `id` is not a UUID |
| POST | `/api/Educations` | 200 created record (server-generated id), 400 validation problem |
| PUT | `/api/Educations/{id}` | 200 empty, 400 if body id differs from path id or record is missing |
| DELETE | `/api/Educations/{id}` | 200 empty, 400 if record is missing |
| GET | `/actuator/health`, `/actuator/health/readiness`, `/actuator/health/liveness` | Health probes |
| GET | `/actuator/prometheus` | Metrics |

Routes are case-insensitive (`/api/educations` works too). Unexpected errors return 500 `application/problem+json`.

## Get started

### Option 1: docker compose

Requires [Docker](https://docs.docker.com/get-docker/).

```bash
cp .env.example .env      # set your own passwords
docker compose up -d --build
```

- Web API: `http://localhost:5000` (Swagger UI at `http://localhost:5000/swagger`, `dev` profile)
- PostgreSQL: `localhost:5433`
- pgAdmin: `http://localhost:8080`

Flyway creates the schema and seed row on startup; there is no separate migration step. Stop with `docker compose down`.

### Option 2: run locally

Requires JDK 21 and Maven 3.9+, plus a PostgreSQL instance (e.g. `docker compose up -d postgres_db`).

```bash
export SPRING_DATASOURCE_URL=jdbc:postgresql://localhost:5433/SampleDB
export SPRING_DATASOURCE_USERNAME=sara
export SPRING_DATASOURCE_PASSWORD=<your password>
mvn spring-boot:run -Dspring-boot.run.profiles=dev
```

### Configuration

| Variable | Default | Purpose |
|---|---|---|
| `SPRING_DATASOURCE_URL` | `jdbc:postgresql://localhost:5433/SampleDB` | JDBC URL |
| `SPRING_DATASOURCE_USERNAME` | `sara` | DB user |
| `SPRING_DATASOURCE_PASSWORD` | (none) | DB password |
| `SERVER_PORT` | `5000` | HTTP port |
| `SPRING_PROFILES_ACTIVE` | (none) | `dev` enables Swagger UI and `/swagger/v1/swagger.json` |

## Tests

```bash
mvn verify
```

Docker must be running: integration and contract tests start PostgreSQL 16 with Testcontainers.

- `DefaultEducationServiceTest`: service logic (Mockito)
- `EducationsControllerTest`: routes, status codes, validation and error handling (MockMvc)
- `EducationApiIntegrationTest`: full CRUD against PostgreSQL with the Flyway schema
- `OpenApiContractTest`: validates `api-spec/openapi.yaml` and checks it matches the implemented operations and payload fields

## Migration notes

- **Database**: same `"Educations"` table, column names, lengths and seed row. Existing databases created by the old EF Core migration are adopted automatically (Flyway `baseline-on-migrate`); `__EFMigrationsHistory` is left in place.
- **Behavior kept on purpose**: POST returns 200 (not 201); GET of an unknown id returns 204; failed PUT/DELETE return 400; non-UUID ids return 404; values longer than the column limits return 500 (database error), as before.
- **Validation messages**: field names in `errors` are camelCase (`degree`) instead of PascalCase (`Degree`), and messages for malformed bodies use the key `body`. Status, `type`, `title` and media type are unchanged.
- **Parity**: `parity/` replays 25 requests against both implementations. Result: 0 mismatches, 5 validation-message-only differences. See [`parity/README.md`](parity/README.md).

## License

MIT, see [LICENSE](LICENSE).
