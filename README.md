# MidgardAddressBook

> A small Blazor Web App address book demonstrating a modern .NET 10 layered architecture
> with Dapper, PostgreSQL, Redis, Autofac, AutoMapper, Serilog, and FluentMigrator.

## Table of Contents

- [Background](#background)
- [Architecture](#architecture)
- [Install](#install)
- [Usage](#usage)
- [Environment Variables](#environment-variables)
- [Deployment](#deployment)
- [Maintainers](#maintainers)
- [Contributing](#contributing)
- [License](#license)

## Background

MidgardAddressBook was originally an ASP.NET Core 3.1 MVC application with EF Core and LibMan-managed
jQuery/Bootstrap assets. It has been rebuilt from the ground up as a modern, production-oriented
sample application that uses the current ValhallaTech stack.

## Architecture

The repository is organized as a solution (`MidgardAddressBook.slnx`) containing four projects under `src/`:

| Project                         | Kind           | Responsibility                                                      |
|---------------------------------|----------------|---------------------------------------------------------------------|
| `MidgardAddressBook.Core`       | Class library  | Domain models, DTOs, and service/repository interfaces.             |
| `MidgardAddressBook.DAL`        | Class library  | Dapper + Npgsql repositories, Redis cache, FluentMigrator migrations. |
| `MidgardAddressBook.BLL`        | Class library  | Service implementations and AutoMapper profiles.                    |
| `MidgardAddressBook.Web`        | Blazor Web App | Interactive Server UI, Autofac DI, Serilog, Bootstrap assets.       |

Key technology choices:

- **.NET 10** for all projects.
- **Dapper + Npgsql** for data access (no EF Core).
- **StackExchange.Redis** for read-through caching of the entry list.
- **FluentMigrator** for schema migrations; migrations run at Web startup.
- **Autofac** as the DI container, integrated with AutoMapper via `AutoMapper.Contrib.Autofac.DependencyInjection`.
- **Serilog** with console sink, configured from `appsettings*.json`.
- **Bootstrap 5.3** shipped via npm, copied into `wwwroot/` at build time.

## Install

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/) (for Bootstrap asset build)
- [Docker](https://www.docker.com/) and Docker Compose (recommended for local dev)

### Build locally

```bash
# Restore + build the whole solution.
dotnet build MidgardAddressBook.slnx -c Release

# Build front-end assets (Bootstrap) manually if you plan to `dotnet run` without Docker.
cd src/MidgardAddressBook.Web
npm install
npm run build
```

## Usage

### Local dev via Docker Compose (recommended)

`docker-compose.yml` stands up the Blazor app alongside PostgreSQL 16 and Redis 7 with named volumes:

```bash
docker compose up --build
# Open http://localhost:8080
```

Compose wires `DATABASE_URL=postgres://midgard:midgard@postgres:5432/midgard` and
`REDIS_URL=redis://redis:6379` into the `web` service. FluentMigrator creates the
`address_book_entries` table on first boot.

### Running the Web project directly

```bash
export DATABASE_URL="postgres://USER:PASSWORD@localhost:5432/midgard"
export REDIS_URL="redis://localhost:6379"
dotnet run --project src/MidgardAddressBook.Web
```

## Environment Variables

| Variable              | Required | Description                                                              |
|-----------------------|:--------:|--------------------------------------------------------------------------|
| `DATABASE_URL`        | ✅       | Render-style Postgres URL (`postgres://user:pass@host:port/db`) **or** a native Npgsql connection string. |
| `REDIS_URL`           | ✅       | `redis://[:password@]host:port` or `rediss://...` for TLS, or a StackExchange.Redis config string. |
| `PORT`                | ⚠️       | Port for Kestrel to bind to. Injected by Render/PaaS; defaults to `8080` in container. |
| `ASPNETCORE_ENVIRONMENT` | ⚠️    | `Development`, `Production`, etc.                                       |

No credentials are hardcoded in source. The Web project parses `DATABASE_URL` / `REDIS_URL` at
startup using `ConnectionStringTranslator` in `MidgardAddressBook.DAL`.

## Deployment

A [Render](https://render.com) blueprint (`render.yaml`) is included that provisions:

- a Docker web service (this app),
- a managed PostgreSQL database,
- a managed Redis (Key-Value) instance.

`DATABASE_URL` and `REDIS_URL` are wired automatically from those services, and the web service
listens on `$PORT` injected by Render.

Dependency updates are automated by **Renovate** (`renovate.json`). NuGet, npm, Dockerfile,
docker-compose, and GitHub Actions managers are enabled with weekly grouped PRs.

## Maintainers

[@ValhallaTech](https://github.com/ValhallaTech)

## Contributing

PRs welcome. The repository follows Conventional Commits and Keep-a-Changelog for the CHANGELOG.

## License

[MIT](LICENSE) © ValhallaTech
