using System.Net;
using System.Net.Http.Json;
using Shouldly;
using OnTime.Application.DTOs.LeadSources;
using OnTime.Tests.Infrastructure;

namespace OnTime.Tests.Flows;

/// <summary>
/// Lead Source maintenance (2026-06-29) — LeadSource is now company-configurable data
/// (LeadSourceOption) instead of a fixed backend enum. See ROADMAP.md.
/// </summary>
[Collection("Integration")]
public class LeadSourceFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public LeadSourceFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task NewCompany_GetsEightDefaultLeadSources()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var resp = await _factory.Client.GetAsync("/api/lead-sources", manager.Token);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var list = await resp.Content.ReadFromJsonAsync<List<LeadSourceOptionDto>>();
        list!.Count.ShouldBe(8);
        list.Select(x => x.Code).ShouldBe(Enumerable.Range(0, 8), ignoreOrder: true);
        list.ShouldAllBe(x => x.IsActive);
    }

    [Fact]
    public async Task Manager_CanCreateRenameAndDeactivate()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var createResp = await _factory.Client.PostAsJsonAsync(
            "/api/lead-sources", new CreateLeadSourceRequest("Google Ads"), manager.Token);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<LeadSourceOptionDto>();
        created!.Code.ShouldBe(8); // next after the 8 seeded defaults (0-7)

        var updateResp = await _factory.Client.PutAsJsonAsync(
            $"/api/lead-sources/{created.Id}", new UpdateLeadSourceRequest("Google Ads (Search)"), manager.Token);
        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<LeadSourceOptionDto>();
        updated!.Name.ShouldBe("Google Ads (Search)");

        var deactivateResp = await _factory.Client.PatchAsJsonAsync(
            $"/api/lead-sources/{created.Id}/active", new SetLeadSourceActiveRequest(false), manager.Token);
        deactivateResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var listResp = await _factory.Client.GetAsync("/api/lead-sources", manager.Token);
        var list = await listResp.Content.ReadFromJsonAsync<List<LeadSourceOptionDto>>();
        list!.Single(x => x.Id == created.Id).IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Salesperson_CanReadButNotWrite()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var sales = await TestHelpers.RegisterSalespersonAsync(_factory.Client, manager.CompanyId!.Value, manager.BrandId!.Value);

        var listResp = await _factory.Client.GetAsync("/api/lead-sources", sales.Token);
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var createResp = await _factory.Client.PostAsJsonAsync(
            "/api/lead-sources", new CreateLeadSourceRequest("Tentativa"), sales.Token);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_CannotEditAnotherCompanysLeadSource()
    {
        var managerA = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var managerB = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var listResp = await _factory.Client.GetAsync("/api/lead-sources", managerA.Token);
        var listA = await listResp.Content.ReadFromJsonAsync<List<LeadSourceOptionDto>>();
        var optionFromA = listA!.First();

        var resp = await _factory.Client.PutAsJsonAsync(
            $"/api/lead-sources/{optionFromA.Id}", new UpdateLeadSourceRequest("Hijacked"), managerB.Token);
        resp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
