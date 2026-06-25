using System.Net;
using System.Net.Http.Json;
using Shouldly;
using OnTimeCRM.Application.DTOs.Users;
using OnTimeCRM.Application.DTOs.Vehicles;
using OnTimeCRM.Tests.Infrastructure;
using PagedResult = OnTimeCRM.Application.Common.PagedResult<OnTimeCRM.Application.DTOs.Vehicles.VehicleModelListDto>;

namespace OnTimeCRM.Tests.Flows;

/// <summary>
/// Flow 11 — User Vehicle Brands (F1)
/// Goal: each user selects the vehicle brands they sell; vehicle model lists are filtered
/// server-side to those brands (empty selection = no filter = all brands).
/// </summary>
[Collection("Integration")]
public class UserVehicleBrandsFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public UserVehicleBrandsFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyVehicleBrands_NewUser_ReturnsEmptyList()
    {
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, auth.UserId);

        var resp = await _factory.Client.GetAsync("/api/users/me/vehicle-brands", auth.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<UserVehicleBrandsDto>();
        dto!.BrandIds.ShouldBeEmpty();
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetMyVehicleBrands_PersistsSelection_AndIsReturnedOnGet()
    {
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, auth.UserId);

        var brandA = await CreateVehicleBrandAsync(auth.Token, "BrandA");
        var brandB = await CreateVehicleBrandAsync(auth.Token, "BrandB");

        var putResp = await _factory.Client.PutAsJsonAsync(
            "/api/users/me/vehicle-brands",
            new UpdateUserVehicleBrandsRequest([brandA, brandB]),
            auth.Token);
        putResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResp = await _factory.Client.GetAsync("/api/users/me/vehicle-brands", auth.Token);
        var dto = await getResp.Content.ReadFromJsonAsync<UserVehicleBrandsDto>();
        dto!.BrandIds.ShouldBe([brandA, brandB], ignoreOrder: true);
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetMyVehicleBrands_CanClearSelection()
    {
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, auth.UserId);
        var brandA = await CreateVehicleBrandAsync(auth.Token, "BrandA");

        await _factory.Client.PutAsJsonAsync(
            "/api/users/me/vehicle-brands", new UpdateUserVehicleBrandsRequest([brandA]), auth.Token);

        // ACT — clear
        var putResp = await _factory.Client.PutAsJsonAsync(
            "/api/users/me/vehicle-brands", new UpdateUserVehicleBrandsRequest([]), auth.Token);
        putResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResp = await _factory.Client.GetAsync("/api/users/me/vehicle-brands", auth.Token);
        var dto = await getResp.Content.ReadFromJsonAsync<UserVehicleBrandsDto>();
        dto!.BrandIds.ShouldBeEmpty();
    }

    // ── Test 4 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetModels_NoSelection_ReturnsAllBrandsModels()
    {
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, auth.UserId);

        var brandA = await CreateVehicleBrandAsync(auth.Token, "BrandA");
        var brandB = await CreateVehicleBrandAsync(auth.Token, "BrandB");
        await CreateVehicleModelAsync(auth.Token, brandA, "ModelA");
        await CreateVehicleModelAsync(auth.Token, brandB, "ModelB");

        var resp = await _factory.Client.GetAsync("/api/vehicles/models", auth.Token);
        var list = await resp.Content.ReadFromJsonAsync<PagedResult>();

        list!.Items.Select(m => m.Name).ShouldContain("ModelA");
        list.Items.Select(m => m.Name).ShouldContain("ModelB");
    }

    // ── Test 5 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetModels_WithSelection_FiltersToSelectedBrandsOnly()
    {
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, auth.UserId);

        var brandA = await CreateVehicleBrandAsync(auth.Token, "BrandA");
        var brandB = await CreateVehicleBrandAsync(auth.Token, "BrandB");
        await CreateVehicleModelAsync(auth.Token, brandA, "ModelA");
        await CreateVehicleModelAsync(auth.Token, brandB, "ModelB");

        await _factory.Client.PutAsJsonAsync(
            "/api/users/me/vehicle-brands", new UpdateUserVehicleBrandsRequest([brandA]), auth.Token);

        // ACT — no explicit brandId filter; should default to the user's selected brands
        var resp = await _factory.Client.GetAsync("/api/vehicles/models", auth.Token);
        var list = await resp.Content.ReadFromJsonAsync<PagedResult>();

        list!.Items.Select(m => m.Name).ShouldContain("ModelA");
        list.Items.Select(m => m.Name).ShouldNotContain("ModelB");
    }

    // ── Test 6 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetModels_WithSelection_ExplicitBrandIdOverridesUserSelection()
    {
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, auth.UserId);

        var brandA = await CreateVehicleBrandAsync(auth.Token, "BrandA");
        var brandB = await CreateVehicleBrandAsync(auth.Token, "BrandB");
        await CreateVehicleModelAsync(auth.Token, brandA, "ModelA");
        await CreateVehicleModelAsync(auth.Token, brandB, "ModelB");

        await _factory.Client.PutAsJsonAsync(
            "/api/users/me/vehicle-brands", new UpdateUserVehicleBrandsRequest([brandA]), auth.Token);

        // ACT — explicit brandId=B should still work even though the user only selected A
        var resp = await _factory.Client.GetAsync($"/api/vehicles/models?brandId={brandB}", auth.Token);
        var list = await resp.Content.ReadFromJsonAsync<PagedResult>();

        list!.Items.Select(m => m.Name).ShouldContain("ModelB");
        list.Items.Select(m => m.Name).ShouldNotContain("ModelA");
    }

    private async Task<Guid> CreateVehicleBrandAsync(string token, string name)
    {
        var resp = await _factory.Client.PostAsJsonAsync(
            "/api/vehicles/brands", new CreateVehicleBrandRequest($"{name}-{Guid.NewGuid():N}", null), token);
        var brand = await resp.Content.ReadFromJsonAsync<VehicleBrandDto>();
        return brand!.Id;
    }

    private async Task CreateVehicleModelAsync(string token, Guid brandId, string name)
    {
        await _factory.Client.PostAsJsonAsync(
            "/api/vehicles/models",
            new CreateVehicleModelRequest(brandId, name, null, null, null, null, null),
            token);
    }
}
