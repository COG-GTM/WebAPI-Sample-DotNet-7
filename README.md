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

Settings live in `src/WebApi/appsettings.json` and can be overridden with environment variables using the standard `Section__Key` form (see `docker-compose.yml`, which sets `ConnectionStrings__DefaultConnection`).

| Setting | Environment variable | Default | Description |
| --- | --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | local postgres on port 5433 | PostgreSQL connection string |
| `RateLimiting:AnonymousRequestsPerMinute` | `RateLimiting__AnonymousRequestsPerMinute` | `60` | Requests per minute allowed per client IP when no API key is sent |
| `RateLimiting:AuthenticatedRequestsPerMinute` | `RateLimiting__AuthenticatedRequestsPerMinute` | `600` | Requests per minute allowed per API key |
| `RateLimiting:ApiKeyHeaderName` | `RateLimiting__ApiKeyHeaderName` | `X-Api-Key` | Header whose value identifies an authenticated client |

### Rate limiting

Every route except the health check (`GET /health`) is rate limited with a one-minute fixed window. Requests carrying the API key header are counted per key; all other requests are counted per client IP. When a limit is exceeded the API responds with `429 Too Many Requests`, a `Retry-After` header (seconds until the window resets) and the body:

```json
{ "error": "rate_limited", "retry_after_seconds": 42 }
```

## Contributions
Contributions are welcomed! If you identify areas for improvement, please feel free to raise an issue or submit a pull request.
