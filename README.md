# OnTimeCRM Backend

ASP.NET Core 8 + EF Core + PostgreSQL backend for OnTimeCRM.

## Structure
- `src/OnTimeCRM.API` - host, controllers, middleware, Program.cs
- `src/OnTimeCRM.Application` - services, DTOs, interfaces
- `src/OnTimeCRM.Domain` - entities and enums
- `src/OnTimeCRM.Infrastructure` - DbContext, EF configuration, repositories
- `src/OnTimeCRM.Tests` - integration tests

## Build and run
```bash
dotnet build .\OnTimeCRM.sln
dotnet run --project .\src\OnTimeCRM.API\OnTimeCRM.API.csproj
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
- Production config is encrypted in `src/OnTimeCRM.API/appsettings_prod.txt`.
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
- `../ONTIMECRM/OnTimeCRM.md`
- `../ONTIMECRM/00-PROJECT/ARCHITECTURE.md`
- `../ONTIMECRM/00-PROJECT/CONVENTIONS.md`
- `../ONTIMECRM/02-DATABASE/API-REFERENCE.md`
