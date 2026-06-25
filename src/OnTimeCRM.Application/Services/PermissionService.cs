using Microsoft.EntityFrameworkCore;
using OnTimeCRM.Application.DTOs.Permissions;
using OnTimeCRM.Application.Interfaces;
using OnTimeCRM.Domain.Entities;

namespace OnTimeCRM.Application.Services;

/// <summary>
/// Default menu routes seeded once. Managers can override CanView/Create/Edit/Delete per role.
/// Backend API enforcement is still done by [ManagerOnly] / [Authorize]; this is UI-level only.
/// </summary>
public class PermissionService : IPermissionService
{
    // All application routes that appear in the nav — deliberately excludes "/admin": that's
    // the cross-tenant platform-admin panel, gated to role 2 only at the policy level
    // (see Program.cs's "AdminOnly" policy). It can never be a per-company, per-role menu
    // permission a Manager configures for their own Salespeople — Admin access isn't tenant
    // data, it's a fixed platform-level constant.
    private static readonly string[] AllRoutes =
    [
        "/dashboard", "/clients", "/proposals", "/sales",
        "/notifications", "/stages", "/vehicles", "/goals",
        "/friends", "/brands", "/access-control",
    ];

    private readonly IAppDbContext _db;
    private readonly IUnitOfWork   _uow;

    public PermissionService(IAppDbContext db, IUnitOfWork uow)
    {
        _db  = db;
        _uow = uow;
    }

    public async Task<IEnumerable<MenuPermissionDto>> GetPermissionsAsync(int role, CancellationToken ct = default)
    {
        // Admin (role=2) always has full access — bypass DB and return computed set
        if (role == 2)
            return AllRoutes.Select(r => new MenuPermissionDto(Guid.Empty, 2, r, true, true, true, true));

        await EnsureSeedAsync(ct);

        var perms = await _db.MenuItemPermissions
            .AsNoTracking()
            .Where(p => p.Role == role)
            .ToListAsync(ct);

        return perms.Select(ToDto);
    }

    public async Task UpdatePermissionsAsync(int role, IEnumerable<UpdateMenuPermissionRequest> updates, CancellationToken ct = default)
    {
        // Admin permissions are computed, not stored — nothing to update
        if (role == 2) return;

        var existing = await _db.MenuItemPermissions
            .Where(p => p.Role == role)
            .ToListAsync(ct);

        foreach (var req in updates)
        {
            var perm = existing.FirstOrDefault(p => p.RouteKey == req.RouteKey);
            if (perm is null) continue;

            perm.CanView   = req.CanView;
            perm.CanCreate = req.CanCreate;
            perm.CanEdit   = req.CanEdit;
            perm.CanDelete = req.CanDelete;
        }

        await _uow.SaveChangesAsync(ct);
    }

    // ── Seeding ───────────────────────────────────────────────────────────────

    private async Task EnsureSeedAsync(CancellationToken ct)
    {
        if (await _db.MenuItemPermissions.AnyAsync(ct)) return;

        foreach (var route in AllRoutes)
        {
            // Manager: full access everywhere
            _db.MenuItemPermissions.Add(new MenuItemPermission
            {
                Role      = 1,   // Manager
                RouteKey  = route,
                CanView   = true,
                CanCreate = true,
                CanEdit   = true,
                CanDelete = true,
            });

            // Salesperson: no access to manager-only routes
            var isAdminRoute = route is "/brands" or "/access-control";
            _db.MenuItemPermissions.Add(new MenuItemPermission
            {
                Role      = 0,   // Salesperson
                RouteKey  = route,
                CanView   = !isAdminRoute,
                CanCreate = !isAdminRoute,
                CanEdit   = !isAdminRoute,
                CanDelete = !isAdminRoute,
            });
        }

        await _uow.SaveChangesAsync(ct);
    }

    private static MenuPermissionDto ToDto(MenuItemPermission p) =>
        new(p.Id, p.Role, p.RouteKey, p.CanView, p.CanCreate, p.CanEdit, p.CanDelete);
}
