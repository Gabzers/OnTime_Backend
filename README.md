# OnTime Backend

ASP.NET Core 8 + EF Core + PostgreSQL backend for OnTime.

## Structure
- `src/OnTime.API` - host, controllers, middleware, Program.cs
- `src/OnTime.Application` - services, DTOs, interfaces
- `src/OnTime.Domain` - entities and enums
- `src/OnTime.Infrastructure` - DbContext, EF configuration, repositories
- `src/OnTime.Tests` - integration tests

## Build and run
```bash
dotnet build .\OnTime.sln
dotnet run --project .\src\OnTime.API\OnTime.API.csproj
```

## Local Docker
```bash
docker compose up -d postgres
```

API default ports:
- `http://localhost:8080`
- Swagger: `/swagger`
- Scalar: `/docs`

## Key rules
- English only for code and docs.
- No EF migrations.
- Production config is encrypted in `src/OnTime.API/appsettings_prod.txt`.
- `Program.cs` is the composition root.
- Module setup happens through each module `Setup.cs`.

## Main flows
- Clients: create client + first proposal atomically.
- Proposals: list, detail, update, lost, convert to sale.
- Sales: list, detail, dashboard.
- Notifications: list, today, overdue-count, create, done/snooze/ignore.
- Vehicles: brands, models, versions, active toggle.
- Users: profile, vehicle brands, manager views.

## Testing
Integration tests only. See the vault docs in `../ONTIMECRM/02-DATABASE/TESTING.md`.

## References
- `../ONTIMECRM/OnTime.md`
- `../ONTIMECRM/00-PROJECT/ARCHITECTURE.md`
- `../ONTIMECRM/00-PROJECT/CONVENTIONS.md`
- `../ONTIMECRM/02-DATABASE/API-REFERENCE.md`
