using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;
using OnTimeCRM.Domain.Enums;
using OnTimeCRM.Tests.Infrastructure;

namespace OnTimeCRM.Tests.Flows;

/// <summary>
/// T1 — Access scope helper.
/// Verifies that Admin (role=2) gets brand-wide results and that a token missing the
/// required "bid" claim is rejected with 403, not silently treated as Guid.Empty.
/// </summary>
[Collection("Integration")]
public class AccessScopeFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public AccessScopeFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_GetClients_SeesBrandWideResults_NotOwnOnly()
    {
        // ARRANGE — register a manager who will be promoted to admin
        var adminAuth = await TestHelpers.RegisterManagerAsync(_factory.Client);

        // Promote to Admin (role=2) directly in DB
        var adminUser = await _factory.Db.Users.FindAsync(adminAuth.UserId);
        adminUser!.Role = UserRole.Admin;
        await _factory.Db.SaveChangesAsync();
        _factory.Db.ChangeTracker.Clear();

        // Re-login to get a fresh token containing role=2
        var adminToken = await TestHelpers.LoginAsync(_factory.Client, adminAuth.Email);

        // Register a salesperson in the same brand, activate their subscription
        var spAuth = await TestHelpers.RegisterSalespersonAsync(
            _factory.Client, adminAuth.CompanyId, adminAuth.BrandId);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, spAuth.UserId);

        // Create a client as the salesperson
        await TestHelpers.CreateClientWithProposalAsync(_factory.Client, spAuth.Token, db: _factory.Db);

        // ACT — admin queries the client list
        var response = await _factory.Client.GetAsync("/api/clients", adminToken);

        // ASSERT — 200 with 1 client (brand-wide; admin didn't create it)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<dynamic>>();
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeEmpty(
            "Admin should see brand-wide results, not only their own (empty) list");
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Manager_WithMissingBidClaim_Returns403_NotSilentEmptyResult()
    {
        // ARRANGE — register an admin, then remove their BrandId so the re-login JWT has no "bid".
        // Using Admin (role=2) avoids the subscription middleware; semantics are the same:
        // any role >= 1 without bid must get 403, not silent empty results.
        var adminAuth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var adminUser = await _factory.Db.Users.FindAsync(adminAuth.UserId);
        adminUser!.Role    = UserRole.Admin;
        adminUser.BrandId  = null; // causes JwtService to omit "bid" claim on next login
        await _factory.Db.SaveChangesAsync();
        _factory.Db.ChangeTracker.Clear();

        // Re-login: token now has role=2 (admin bypass) but no "bid" claim
        var tokenWithoutBid = await TestHelpers.LoginAsync(_factory.Client, adminAuth.Email);

        // ACT
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/clients");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenWithoutBid);
        var response = await _factory.Client.SendAsync(request);
        // ASSERT — must be 403 (AUTH_FORBIDDEN), NOT 200 with 0 rows
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "A token missing the 'bid' claim must be rejected, not silently return 0 results");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("AUTH_FORBIDDEN");
    }

}
