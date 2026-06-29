using System.Net;
using System.Net.Http.Json;
using Shouldly;
using OnTime.Application.DTOs.Users;
using OnTime.Tests.Infrastructure;

namespace OnTime.Tests.Flows;

/// <summary>
/// Profile self-edit (2026-06-29) — PUT /api/users/me now also accepts an email change, with a
/// USER_EMAIL_TAKEN (409) guard so a user can't take over another account's email. See ROADMAP.md.
/// </summary>
[Collection("Integration")]
public class ProfileSelfEditFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public ProfileSelfEditFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CanChangeOwnEmail()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, manager.UserId);

        var newEmail = $"{Guid.NewGuid():N}@example.com";
        var resp = await _factory.Client.PutAsJsonAsync(
            "/api/users/me", new UpdateUserRequest(manager.Email.Split('@')[0], null, newEmail), manager.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<UserDto>();
        dto!.Email.ShouldBe(newEmail.ToLower());

        // The new email actually works for login.
        var loginToken = await TestHelpers.LoginAsync(_factory.Client, newEmail);
        loginToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CannotChangeToAnotherUsersEmail()
    {
        var managerA = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var managerB = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, managerB.UserId);

        var resp = await _factory.Client.PutAsJsonAsync(
            "/api/users/me", new UpdateUserRequest("Manager B", null, managerA.Email), managerB.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var body = await resp.Content.ReadAsStringAsync();
        body.ShouldContain("USER_EMAIL_TAKEN");
    }

    [Fact]
    public async Task NotChangingEmail_LeavesItUntouched()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);
        await TestHelpers.ActivateSubscriptionDirectAsync(_factory.Db, manager.UserId);

        var resp = await _factory.Client.PutAsJsonAsync(
            "/api/users/me", new UpdateUserRequest("New Name", "351911111111"), manager.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<UserDto>();
        dto!.Email.ShouldBe(manager.Email.ToLower());
        dto.FullName.ShouldBe("New Name");
    }
}
