using System.Net;
using System.Net.Http.Json;
using Shouldly;
using OnTimeCRM.Tests.Infrastructure;

namespace OnTimeCRM.Tests.Flows;

/// <summary>
/// Flow — Trial access enforcement
/// A PendingActivation user whose TrialEndsAt is in the future must have
/// full read+write access (14-day free trial promise).
/// </summary>
[Collection("Integration")]
public class TrialAccessFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public TrialAccessFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActiveTrial_AllowsReadOperations()
    {
        // ARRANGE — register (PendingActivation + TrialDays=0 in test), push trial forward
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await SetTrialEndsAtAsync(auth.UserId, days: 14);

        var token = await TestHelpers.LoginAsync(_factory.Client, auth.Email);

        // ACT
        var resp = await _factory.Client.GetAsync("/api/clients", token);

        // ASSERT
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActiveTrial_AllowsWriteOperations()
    {
        // ARRANGE
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await SetTrialEndsAtAsync(auth.UserId, days: 14);

        var token = await TestHelpers.LoginAsync(_factory.Client, auth.Email);

        // ACT
        var postResp = await _factory.Client.PostAsJsonAsync(
            "/api/clients",
            new { FullName = "Trial Test", Phone = "351910000099", LeadSource = 0, BusinessType = 0, PaymentType = 0 },
            token);

        // ASSERT — 200 or 201
        ((int)postResp.StatusCode).ShouldBeInRange(200, 201);
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExpiredTrial_PendingActivation_BlocksAllAccess()
    {
        // ARRANGE — AuthService hardcodes 14 days; manually push TrialEndsAt to the past.
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await SetTrialEndsAtAsync(auth.UserId, days: -1);

        var token = await TestHelpers.LoginAsync(_factory.Client, auth.Email);

        // ACT
        var getResp = await _factory.Client.GetAsync("/api/clients", token);
        var postResp = await _factory.Client.PostAsJsonAsync(
            "/api/clients",
            new { FullName = "Blocked", Phone = "351910000098", LeadSource = 0, BusinessType = 0, PaymentType = 0 },
            token);

        // ASSERT
        getResp.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
        postResp.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private async Task SetTrialEndsAtAsync(Guid userId, int days)
    {
        var user = await _factory.Db.Users.FindAsync(userId);
        user!.TrialEndsAt = DateTimeOffset.UtcNow.AddDays(days);
        await _factory.Db.SaveChangesAsync();
        _factory.Db.ChangeTracker.Clear();
    }
}
