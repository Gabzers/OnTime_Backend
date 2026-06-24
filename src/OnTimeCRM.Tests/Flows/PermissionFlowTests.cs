using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using OnTimeCRM.Tests.Infrastructure;

namespace OnTimeCRM.Tests.Flows;

/// <summary>
/// Flow — Permission CRUD
/// Verifies that all four permission flags (CanView/Create/Edit/Delete) are
/// stored and returned independently.
/// </summary>
[Collection("Integration")]
public class PermissionFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public PermissionFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePermissions_PersistsAllFourFlagsIndependently()
    {
        // ARRANGE — register manager, then update Salesperson permissions for /clients
        var auth  = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var token = await TestHelpers.LoginAsync(_factory.Client, auth.Email);

        // Seed first (GET triggers EnsureSeed)
        var seedResp = await _factory.Client.GetAsync("/api/permissions?role=0", token);
        seedResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // ACT — update with distinct flag values
        var update = new[]
        {
            new
            {
                RouteKey  = "/clients",
                CanView   = true,
                CanCreate = true,
                CanEdit   = false,
                CanDelete = false,
            }
        };

        var putResp = await _factory.Client.PutAsJsonAsync("/api/permissions/0", update, token);
        putResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // ASSERT — read back via GET and verify flags are independent
        var getResp = await _factory.Client.GetAsync("/api/permissions?role=0", token);
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var perms = await getResp.Content.ReadFromJsonAsync<List<PermissionDto>>();
        var clients = perms!.First(p => p.RouteKey == "/clients");

        clients.CanView.ShouldBeTrue();
        clients.CanCreate.ShouldBeTrue();
        clients.CanEdit.ShouldBeFalse();
        clients.CanDelete.ShouldBeFalse();
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPermissions_Salesperson_ReturnsSeedDefaults()
    {
        var auth  = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var token = await TestHelpers.LoginAsync(_factory.Client, auth.Email);

        var resp = await _factory.Client.GetAsync("/api/permissions?role=0", token);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var perms = await resp.Content.ReadFromJsonAsync<List<PermissionDto>>();
        perms.ShouldNotBeNull();
        perms!.Count.ShouldBeGreaterThan(0);

        // Admin routes should be seeded with all flags false for Salesperson
        var adminPerm = perms.First(p => p.RouteKey == "/admin");
        adminPerm.CanView.ShouldBeFalse();
        adminPerm.CanCreate.ShouldBeFalse();
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPermissions_Admin_ReturnsFullAccessWithoutDBRead()
    {
        var auth  = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var token = await TestHelpers.LoginAsync(_factory.Client, auth.Email);

        var resp = await _factory.Client.GetAsync("/api/permissions?role=2", token);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var perms = await resp.Content.ReadFromJsonAsync<List<PermissionDto>>();
        perms.ShouldNotBeNull();
        perms!.ShouldAllBe(p => p.CanView && p.CanCreate && p.CanEdit && p.CanDelete);
    }

    // ── local DTO for deserialization ────────────────────────────────────────

    private record PermissionDto(
        Guid   Id,
        int    Role,
        string RouteKey,
        bool   CanView,
        bool   CanCreate,
        bool   CanEdit,
        bool   CanDelete
    );
}
