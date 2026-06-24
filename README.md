# OnTimeCRM — Backend

REST API for **OnTimeCRM**, a multi-tenant CRM for car dealerships. Built with ASP.NET Core 8 on Clean Architecture, backed by PostgreSQL, with automated follow-up notifications as its core feature.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![xUnit](https://img.shields.io/badge/xUnit-tested-25A162?logo=dotnet&logoColor=white)](https://xunit.net/)
[![Testcontainers](https://img.shields.io/badge/Testcontainers-Docker-2496ED?logo=docker&logoColor=white)](https://testcontainers.com/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License](https://img.shields.io/badge/license-private-lightgrey)]()

> Part of the OnTimeCRM system. See the [root repository](https://github.com/Gabzers/OnTimeCRM) for full-stack setup, and [OnTimeCRM_Frontend](https://github.com/Gabzers/OnTimeCRM_Frontend) for the SPA client.

---

## Overview

OnTimeCRM helps car-dealership salespeople manage their sales pipeline without losing track of follow-ups. The backend exposes a REST API covering the full client lifecycle — lead intake, pipeline stages, proposals, sales, and the automated notification engine that drives the product's core value: **never miss a follow-up.**

The system is multi-tenant by design: each **Company** owns one or more **Brands**, each Brand has its own **Users** (Managers and Salespeople), and every entity is scoped accordingly through JWT claims rather than database-level tenant IDs scattered across queries.

## Architecture

Clean Architecture with strict one-way dependencies:

```
Domain  ←  Application  ←  Infrastructure  ←  API
```

| Layer | Responsibility | Must not contain |
|---|---|---|
| **Domain** | Entities, enums — no behavior, no dependencies | Business logic, persistence |
| **Application** | Services, DTOs, repository interfaces, access-scope helpers | EF Core, HTTP concerns |
| **Infrastructure** | EF Core `DbContext`, repositories, PostgreSQL functions, JWT/password services | Business rules |
| **API** | Controllers, middleware, DI composition | Business logic |

```
src/
├── OnTimeCRM.Domain/
│   ├── Entities/        # Company, Brand, User, Client, Proposal, Sale, Notification, ...
│   ├── Enums/           # UserRole, UserAccountStatus, NotificationStatus, ...
│   └── Common/          # BaseEntity
├── OnTimeCRM.Application/
│   ├── DTOs/             # Per-resource request/response records
│   ├── Services/         # Business logic (AuthService, ClientService, NotificationService, ...)
│   ├── Interfaces/       # Repository + service contracts
│   └── Common/           # ClaimsPrincipalExtensions, AccessScope, ApiException
├── OnTimeCRM.Infrastructure/
│   ├── Persistence/       # AppDbContext, DatabaseInitializer, EF configurations
│   ├── Repositories/      # EF Core + PostgreSQL function-backed repositories
│   └── Security/          # Pbkdf2PasswordHasher, JwtService
├── OnTimeCRM.API/
│   ├── Controllers/       # 16 controllers — Auth, Clients, Proposals, Sales, ...
│   ├── Middleware/        # ErrorHandlingMiddleware, SubscriptionAccessMiddleware
│   └── Program.cs
└── OnTimeCRM.Tests/
    ├── Flows/              # 15 end-to-end xUnit flow suites
    └── Infrastructure/     # TestWebAppFactory, WireMock setup, test helpers
```

### Data layer: stored functions vs. EF Core

Read/write logic is split by complexity rather than forced into one pattern:

- **Simple single-entity CRUD** → EF Core directly in services.
- **Complex / multi-table / aggregate logic** → PostgreSQL stored functions (`fn_*`), e.g. `fn_get_clients_paged`, `fn_convert_proposal_to_sale`, `fn_get_dashboard_kpis`.

The database has **no EF Core migrations**. `DatabaseInitializer` calls `EnsureCreated()` at startup, detects schema drift, and automatically drops and recreates the schema plus all `fn_*` functions — appropriate for active development, called out explicitly as a pre-production change in this project's internal docs.

### Tenant isolation

Every authenticated request carries a JWT with `sub` (UserId), `cid` (CompanyId), `bid` (BrandId), and `role` claims. Controllers never read these claims directly — they go through a single `AccessScope` helper:

```csharp
public readonly record struct AccessScope(Guid UserId, Guid? BrandId, Guid? CompanyId, int Role)
{
    public bool IsAdmin          => Role == 2;
    public bool IsManagerOrAdmin => Role >= 1;
    public Guid? ManagerBrandScope =>
        IsManagerOrAdmin ? (BrandId ?? throw new ApiException(ApiErrorCatalog.AUTH_FORBIDDEN))
                          : null;
}
```

- **Salesperson** (role 0) — sees only their own records.
- **Manager** (role ≥ 1) — sees all records within their Brand.
- **Admin** (role 2) — bypasses subscription and permission checks entirely.

A missing or malformed claim throws `AUTH_FORBIDDEN` rather than silently falling back to an empty filter — a deliberate fix over an earlier pattern that masked authorization bugs as "0 results."

## Key features

- **Automated follow-up notifications** — stage-triggered templates (e.g. "remind 2 days after a test drive") plus manual reminders, with daily digest, snooze, and overdue tracking.
- **Sales pipeline** — configurable per-user stages, Client → Proposal → Sale conversion, full stage-change history.
- **Role-based access control** — per-menu-item permission flags (`CanRead`/`CanEdit`) independently configurable per role.
- **Goals & KPIs** — dashboard metrics (conversion rate, hot deals, monthly sales, commission) and personal goal tracking.
- **Social layer** — friend requests between salespeople with opt-in public profiles.
- **Localization-ready API** — `/api/i18n` serves versioned translation maps (pt-PT primary, en-US fallback) so the frontend never hardcodes copy.
- **Subscription gating** *(in progress)* — `SubscriptionAccessMiddleware` enforces trial/active/expired access tiers; Stripe and Ifthenpay clients are stubbed and exercised against WireMock in tests, with no live payment provider wired yet.

## API surface

16 controllers, ~70 endpoints. Representative routes:

| Resource | Route prefix | Examples |
|---|---|---|
| Auth | `/api/auth` | `register-manager`, `register`, `login` |
| Clients | `/api/clients` | paged list, create, `{id}/history`, stage change |
| Proposals | `/api/proposals` | create, `{id}/convert`, `{id}/mark-lost` |
| Sales | `/api/sales` | paged list, detail |
| Notifications | `/api/notifications` | `today`, `overdue-count`, `{id}/done`, `{id}/snooze` |
| Stages | `/api/stages` | CRUD, `reorder`, templates |
| Vehicles | `/api/vehicles` | brands, models |
| Users / Brands | `/api/users`, `/api/brands` | manager-scoped CRUD |
| Goals | `/api/user-goals` | CRUD + live progress |
| Permissions | `/api/permissions` | per-role menu flags |
| Friends | `/api/friends` | requests, accept/reject |
| Subscription | `/api/subscription` | status, payments |
| i18n | `/api/i18n` | versioned translation map |

Full reference: Swagger UI at `/swagger` when running locally, or Scalar at `/scalar`.

## Authentication & security

- **JWT Bearer** tokens (`sub`, `cid`, `bid`, `role` claims), validated on issuer/audience/lifetime/signing key.
- **PBKDF2-SHA256** password hashing, 10,000 iterations, per-user salt.
- **Policy-based authorization** — a `ManagerOnly` policy gates manager/admin-only endpoints.
- Structured error responses (`{ code, message, class, traceId }`) via a central `ErrorHandlingMiddleware`, mapping both C# exceptions and Postgres `RAISE EXCEPTION` codes.

## Testing

Integration-first: every flow test boots the **real** ASP.NET Core pipeline against a **real** PostgreSQL instance.

```
OnTimeCRM.Tests/
├── Flows/            # Auth, AccessScope, ClientPipeline, Dashboard, Notifications,
│                      # ProposalSale, Subscription, TenantIsolation, ...
└── Infrastructure/   # TestWebAppFactory, ExternalApiMocks, TestHelpers
```

- **Testcontainers** spins up a disposable `postgres:16-alpine` container per test run.
- **Respawn** resets table data between tests without re-creating the schema.
- **WireMock.Net** mocks Stripe and Ifthenpay so payment flows are tested without hitting a real provider.
- **Bogus** generates realistic Portuguese-locale fake data.
- **Shouldly** for assertions.

```bash
cd src/OnTimeCRM.Tests
dotnet test
```

Docker must be running — Testcontainers needs it to provision the test database.

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL and for running the integration tests)

### Run with Docker Compose

```bash
cp .env.example .env   # see root repo for the full multi-service compose file
docker-compose up -d
```

The API will be available at `http://localhost:8080`, with Swagger at `http://localhost:8080/swagger`.

### Run locally against a containerized database

```bash
docker run -d --name ontimecrm-postgres \
  -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=ontimecrm \
  -p 5434:5432 postgres:16-alpine

cd src/OnTimeCRM.API
dotnet run
```

On first run, `DatabaseInitializer` creates the schema and seeds vehicle catalogue data automatically — no manual migration step required.

### Configuration

Key settings (via `appsettings.json`, environment variables, or `.env` with Docker Compose):

| Setting | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` / `Jwt__Issuer` / `Jwt__Audience` | JWT signing configuration |
| `Cors__AllowedOrigins` | Allowed frontend origins |
| `AdminBootstrap__*` | Optional bootstrap admin account created on first run |

## Related repositories

- [OnTimeCRM](https://github.com/Gabzers/OnTimeCRM) — root repository, full-stack Docker Compose orchestration
- [OnTimeCRM_Frontend](https://github.com/Gabzers/OnTimeCRM_Frontend) — React + TypeScript SPA client

---

*Built as a personal/academic project by [Gabriel Proença](https://github.com/Gabzers), Full Stack Developer (.NET / C# / SQL Server) and MSc Software Engineering student at ISEP.*
