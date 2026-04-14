## ASP.NET Core Web API Sample

This repository is a sample ASP.NET Core Web API (.NET 10) project.

## Features & Technologies
- ASP.NET Core Web API (.NET 10)
- Entity Framework Core 10
- Clean Architecture
- Unit Of Work Pattern
- Repository Service Pattern
- TDD
- PostgreSQL
- Docker
- Built-in OpenAPI support

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Docker](https://docs.docker.com/get-docker/)

## Get started

#### 1. Clone the repository

```
git clone https://github.com/COG-GTM/WebAPI-Sample-DotNet-7.git
```

#### 2. Configure environment variables

Copy the example environment file and adjust values if needed:

```
cp .env.example .env
```

#### 3. Start with docker compose

Run the following command in project directory:

```
docker compose up -d
```

Docker compose in this project includes 3 services: web API application, postgres and pgadmin4.

- Web API application will be running and listening at `http://localhost:5000`

- Postgres database will be listening at `http://localhost:5433`

- PgAdmin4 web interface will be listening at `http://localhost:8080`

- OpenAPI document available at `http://localhost:5000/openapi/v1.json`

To apply your modified code, you can add build option:

```
docker compose up -d --build
```

To stop and remove all containers, use the following command:

```
docker compose down
```


#### 4. Run the migrations

Open Sample.sln file in Visual Studio or use the CLI:

```
dotnet ef database update --project src/Infrastructure --startup-project src/WebApi
```

This command will generate the database schema in postgres container.

## Contributions
Contributions are welcomed! If you identify areas for improvement, please feel free to raise an issue or submit a pull request.
