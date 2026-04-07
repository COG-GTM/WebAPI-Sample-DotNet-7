# Education API (Go)

A Go implementation of the Education CRUD API, mirroring the existing .NET Clean Architecture project.

## Project Structure

```
go-api/
├── cmd/server/main.go          # Application entry point
├── internal/
│   ├── domain/education.go     # Core domain entity
│   ├── dto/education.go        # Data-transfer objects & converters
│   ├── repository/education.go # Data-access interface & PostgreSQL stub
│   ├── service/education.go    # Business-logic interface & implementation
│   └── handler/education.go    # HTTP handlers (chi router)
├── migrations/                 # SQL migration files
├── Dockerfile                  # Multi-stage container build
├── go.mod                      # Go module definition
└── README.md                   # This file
```

## Prerequisites

- Go 1.22+
- PostgreSQL (the same instance used by the .NET project works fine)

## Running

```bash
cd go-api

# Set DATABASE_URL to your PostgreSQL DSN (see docker-compose.yml for local credentials):
export DATABASE_URL="postgres://<user>:<password>@localhost:5433/SampleDB?sslmode=disable"
go run ./cmd/server
```

The server starts on **:8080**.

## Environment Variables

| Variable       | Description                        | Default                                                                   |
| -------------- | ---------------------------------- | ------------------------------------------------------------------------- |
| `DATABASE_URL` | PostgreSQL connection string (DSN) | *(required – see `docker-compose.yml` for local credentials)* |

## API Endpoints

| Method   | Path                    | Description             |
| -------- | ----------------------- | ----------------------- |
| `GET`    | `/health`               | Database health check   |
| `GET`    | `/api/educations`       | List all educations     |
| `GET`    | `/api/educations/{id}`  | Get education by ID     |
| `POST`   | `/api/educations`       | Create a new education  |
| `PUT`    | `/api/educations/{id}`  | Update an education     |
| `DELETE` | `/api/educations/{id}`  | Delete an education     |

## Migrations

Apply the initial migration against your PostgreSQL database:

```bash
psql "$DATABASE_URL" -f migrations/001_create_educations.up.sql
```

To roll back:

```bash
psql "$DATABASE_URL" -f migrations/001_create_educations.down.sql
```

## Docker

```bash
cd go-api
docker build -t education-api .
docker run -p 8080:8080 -e DATABASE_URL="postgres://..." education-api
```
