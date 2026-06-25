using System.Net;
using System.Net.Http.Json;
using Shouldly;
using OnTimeCRM.Application.DTOs.Companies;
using OnTimeCRM.Domain.Enums;
using OnTimeCRM.Tests.Infrastructure;

namespace OnTimeCRM.Tests.Flows;

/// <summary>
/// Flow — Admin panel (cross-tenant company management) must be restricted to platform
/// Admins only. A regular Manager is a customer of the SaaS, not its operator — letting
/// them list/disable/edit OTHER companies via this panel is a multi-tenancy violation.
/// </summary>
[Collection("Integration")]
public class AdminFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public AdminFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RegularManager_CannotListOtherCompanies_ViaAdminPanel()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var resp = await _factory.Client.GetAsync("/api/admin/companies", manager.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegularManager_CannotDisableAnotherCompany()
    {
        var attacker = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var victim = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var resp = await _factory.Client.PatchAsJsonAsync(
            $"/api/admin/companies/{victim.CompanyId}/active",
            new { IsActive = false },
            attacker.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformAdmin_CanListAllCompanies()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await PromoteToAdminAsync(manager.UserId);
        var adminToken = await TestHelpers.LoginAsync(_factory.Client, manager.Email);

        var resp = await _factory.Client.GetAsync("/api/admin/companies", adminToken);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var companies = await resp.Content.ReadFromJsonAsync<List<CompanyAdminDto>>();
        companies.ShouldNotBeNull();
    }

    private async Task PromoteToAdminAsync(Guid userId)
    {
        var user = await _factory.Db.Users.FindAsync(userId);
        user!.Role = UserRole.Admin;
        await _factory.Db.SaveChangesAsync();
        _factory.Db.ChangeTracker.Clear();
    }
}
