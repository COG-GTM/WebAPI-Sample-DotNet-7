## ASP.NETCore Web API Sample

This repository is a sample ASP.NET Core Web API (.NET 7) project.

## Features & Technologies
- ASP.NET Core Web API
- Entity Framework Core
- Clean Architecture
- Unit Of Work Pattern
- Repository Service Pattern
- TDD
- PostgreSQL
- Docker

## Get started

#### 1. Clone the repository

```
git clone https://github.com/SaraRasoulian/WebAPI-Sample-DotNet-7.git
```
#### 2. Start with docker compose

Make sure [docker](https://docs.docker.com/get-docker/) is installed on your machine.

Run the following command in project directory:

```
docker-compose up -d
```

Docker compose in this project includes 3 services: web API application, postgres and pgadmin4.

- Web API application will be running and listening at `http://localhost:5000`

- Postgres database will be listening at `http://localhost:5433`

- PgAdmin4 web interface will be listening at `http://localhost:8080`


To apply your modified code, you can add build option:

```
  docker-compose up -d --build
```

To stop and remove all containers, use the following command:

```
  docker-compose down
```


#### 3. Run the migrations

Open Sample.sln file in visual studio, then in package manager console tab, run:

```
update-database
```

This command will generate the database schema in postgres container.

## Configuration

Settings are read from `src/WebApi/appsettings.json` and can be overridden with environment variables using `__` as the section separator (as `docker-compose.yml` does for `ConnectionStrings__DefaultConnection`).

| Setting | Environment variable | Default | Description |
| --- | --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | local postgres | PostgreSQL connection string |
| `RateLimiting:UnauthenticatedRequestsPerMinute` | `RateLimiting__UnauthenticatedRequestsPerMinute` | `60` | Requests per minute allowed per client IP when no API key is sent |
| `RateLimiting:AuthenticatedRequestsPerMinute` | `RateLimiting__AuthenticatedRequestsPerMinute` | `600` | Requests per minute allowed per API key |
| `RateLimiting:ApiKeyHeaderName` | `RateLimiting__ApiKeyHeaderName` | `X-Api-Key` | Header whose value identifies an authenticated client |
| `RateLimiting:ApiKeys` | `RateLimiting__ApiKeys__0`, `__1`, ... | `[]` | Allowlist of API keys eligible for the authenticated limit; when empty any non-empty header value counts |

Both limits must be at least 1 and the header name must be non-empty; the application refuses to start otherwise.

### Rate limiting

Every route except the health check (`GET /health`) is rate limited with a fixed one-minute window. Requests carrying a recognised API key header are counted per key; all others (including unknown keys when `ApiKeys` is set) are counted per client IP. The IP is `HttpContext.Connection.RemoteIpAddress`; when running behind a reverse proxy, configure [forwarded headers](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer) so clients are not all counted as the proxy. When a limit is exceeded the API responds with `429 Too Many Requests`, a `Retry-After` header (seconds) and the body:

```json
{ "error": "rate_limited", "retry_after_seconds": 42 }
```

Example override in `docker-compose.yml`:

```yaml
environment:
  - RateLimiting__UnauthenticatedRequestsPerMinute=120
  - RateLimiting__AuthenticatedRequestsPerMinute=1200
```

## Contributions
Contributions are welcomed! If you identify areas for improvement, please feel free to raise an issue or submit a pull request.
