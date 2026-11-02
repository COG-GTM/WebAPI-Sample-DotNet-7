# .NET 7 is EOL and its images are no longer rebuilt; use the Debian 12 variant and apply OS security updates at build time.
FROM mcr.microsoft.com/dotnet/aspnet:7.0-bookworm-slim AS base
RUN apt-get update \
    && apt-get upgrade -y --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:7.0-bookworm-slim AS build
RUN pwd && ls /
WORKDIR /src
COPY ["src/WebApi/WebApi.csproj", "src/WebApi/"]
RUN pwd && ls
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "./src/WebApi/WebApi.csproj"
COPY . .
WORKDIR "/src/src/WebApi"
RUN dotnet build "WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]