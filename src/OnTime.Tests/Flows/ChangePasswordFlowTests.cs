using System.Net;
using OnTime.Application.DTOs.Users;
using OnTime.Tests.Infrastructure;
using Shouldly;

namespace OnTime.Tests.Flows;

/// <summary>
/// Profile password change (2026-06-29) — PUT /api/users/me/password, gated on the current
/// password matching. See ROADMAP.md.
/// </summary>
[Collection("Integration")]
public class ChangePasswordFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public ChangePasswordFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CanChangeOwnPassword_AndLoginWithNewOne()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var resp = await _factory.Client.PutAsJsonAsync(
            "/api/users/me/password", new ChangePasswordRequest("Teste123!", "NovaPass456!"), manager.Token);
        resp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var newToken = await TestHelpers.LoginAsync(_factory.Client, manager.Email, "NovaPass456!");
        newToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task WrongCurrentPassword_Returns422()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var resp = await _factory.Client.PutAsJsonAsync(
            "/api/users/me/password", new ChangePasswordRequest("WrongPassword!", "NovaPass456!"), manager.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadAsStringAsync();
        body.ShouldContain("USER_CURRENT_PASSWORD_INVALID");
    }
}
