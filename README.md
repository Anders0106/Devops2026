# Chirp

A lightweight Twitter-style microblogging platform where users can post short messages ("cheeps"), follow other users, and comment on posts. Built with ASP.NET Core and deployed via Docker Swarm on DigitalOcean.

## Description

Chirp is a social microblogging web application inspired by Twitter. It supports:

- **Cheeps** — short public messages with optional image attachments
- **Timelines** — public timeline, per-user timeline, and a personalized "my timeline"
- **Following** — follow/unfollow other users to curate your feed
- **Comments** — reply to individual cheeps
- **User accounts** — registration, login, and identity management via ASP.NET Core Identity

The application follows an onion architecture with clearly separated layers for domain logic, data access, services, and presentation.

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Language** | C# / .NET 8 |
| **Web framework** | ASP.NET Core (Razor Pages + MVC) |
| **Database** | PostgreSQL 17 |
| **ORM** | Entity Framework Core 8 (Npgsql) |
| **Authentication** | ASP.NET Core Identity |
| **Containerization** | Docker, Docker Compose, Docker Swarm |
| **Reverse proxy** | Caddy 2 |
| **Infrastructure** | Terraform + DigitalOcean |
| **CI/CD** | GitHub Actions + GitHub Container Registry |
| **Monitoring** | Prometheus, Grafana, Loki, Promtail |
| **Security scanning** | Semgrep, DevSkim |

## Project Structure

```
├── src/
│   ├── Chirp.Core/           # Domain models and DTOs
│   ├── Chirp.Repositories/   # EF Core DbContext, repositories, migrations
│   ├── Chirp.Services/       # Business logic services
│   └── Chirp.Razor/          # ASP.NET Core web app (Razor Pages, controllers, wwwroot)
├── test/
│   └── Chirp.Tests/          # Unit, integration, E2E, and UI tests
├── infrastructure/            # Terraform IaC for DigitalOcean
├── monitoring/                # Prometheus, Grafana, Loki, Promtail configs
├── scripts/                   # Database seed data
├── docs/                      # Architecture report, diagrams
├── docker-compose.yml         # Local development compose
├── docker-compose.swarm.yml   # Production Swarm stack
├── Dockerfile                 # Multi-stage app container build
├── Caddyfile                  # Reverse proxy configuration
└── Makefile                   # Common build/test shortcuts
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 17+](https://www.postgresql.org/download/)
- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/install/) (for containerized workflows)
- [Terraform](https://developer.hashicorp.com/terraform/downloads) (only for infrastructure provisioning)
- [Node.js](https://nodejs.org/) (only for Playwright browser installation in tests)

## Installation

Clone the repository:

```bash
git clone https://github.com/Anders0106/Devops2026.git
cd Devops2026
```

Restore .NET dependencies:

```bash
dotnet restore
```

## Usage

### Option 1: Run with Docker Compose (recommended)

This starts the web app and a PostgreSQL database with no local setup required beyond Docker:

```bash
docker compose up
```

The app is available at **http://localhost:8080**.

### Option 2: Run locally with .NET CLI

Ensure PostgreSQL is running on `localhost:5432` with a database named `chirp` (credentials default to `postgres`/`postgres` per `appsettings.json`). Then:

```bash
dotnet run --project src/Chirp.Razor
```

The app starts on the URL configured by your environment (default https://localhost:5001 or http://localhost:5000).

### Makefile shortcuts

```bash
make build     # Build the solution
make test      # Run all tests
make clean     # Clean build artifacts
make restore   # Restore NuGet packages
```

## Deployment

### Docker Swarm (production)

The full production stack includes the app, PostgreSQL, Caddy reverse proxy, monitoring (Prometheus, Grafana, Loki, Promtail), a Postgres exporter, and Shepherd for automatic image updates.

```bash
docker swarm init
docker stack deploy -c docker-compose.swarm.yml chirp
```

Container images are published to GHCR automatically on GitHub Release via the `release-docker.yml` workflow.

### Infrastructure with Terraform

Provision a DigitalOcean droplet with Docker Swarm pre-configured:

```bash
cd infrastructure
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your DigitalOcean token and preferences
terraform init
terraform apply
```

Required variables: `do_token`, `github_image_repo`. See `variables.tf` for all options and defaults.

## Testing

The test suite covers multiple layers:

| Type | Framework | Description |
|------|-----------|-------------|
| **Unit** | xUnit | Domain logic with in-memory SQLite |
| **Integration** | xUnit + WebApplicationFactory | HTTP pipeline with test database |
| **E2E** | NUnit + Playwright | Full browser-driven scenarios |
| **UI** | NUnit + Playwright | Page interaction and rendering |

Run all tests:

```bash
make test
```

For E2E/UI tests, install Playwright browsers first:

```bash
npx playwright@1.49.0 install --with-deps chromium
```

## CI/CD

GitHub Actions runs on every push and pull request:

1. **Semgrep** — security scanning (OWASP Top 10, C#, Dockerfile rules)
2. **Build** — `dotnet build` the full solution
3. **Seed** — loads test data into a PostgreSQL service container
4. **Test** — runs the complete test suite including Playwright browser tests

A separate **DevSkim** workflow scans for security anti-patterns on pushes to `main`.

On GitHub Release, the **release-docker** workflow builds and pushes the container image to `ghcr.io/anders0106/devops2026` with the release tag (and `latest` for non-prerelease).

## Monitoring

The Swarm stack includes a full observability setup:

- **Prometheus** (port 9090) — scrapes application metrics from `/metrics` and PostgreSQL metrics from `postgres-exporter`
- **Grafana** (port 3000) — dashboards and alerting, pre-configured with Prometheus and Loki datasources
- **Loki + Promtail** — centralized log aggregation from all Docker containers

All monitoring ports are bound to `127.0.0.1` and are not publicly exposed.

## Contributing

Contributions are welcome. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-change`)
3. Make your changes and ensure tests pass (`make test`)
4. Commit and push to your fork
5. Open a pull request

## Authors

- **Anders0106** — [GitHub](https://github.com/Anders0106)
- **Alex Op** — [GitHub](https://github.com/alexop1000)
- **Phillip** — [GitHub](https://github.com/BuiltByPhillip)
- **alefr** — [GitHub](https://github.com/alexander1519F)
- **davn** — [GitHub](https://github.com/TheDavidNN)

## License

This project is released into the public domain under the [Unlicense](LICENSE).
