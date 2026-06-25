using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using OnTimeCRM.Application.Common;
using OnTimeCRM.Application.DTOs.Admin;
using OnTimeCRM.Domain.Entities;
using OnTimeCRM.Tests.Infrastructure;

namespace OnTimeCRM.Tests.Flows;

/// <summary>
/// Flow — Error logging. Every error response the API returns (ApiException, DB conflict, or
/// unhandled exception) must be persisted by ErrorHandlingMiddleware so there's a record —
/// see CLAUDE.md's i18n-validation precedent: this is the equivalent safeguard for errors.
/// </summary>
[Collection("Integration")]
public class ErrorLogFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public ErrorLogFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ApiException_IsPersistedAsErrorLog_WithRequestDetails()
    {
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var resp = await _factory.Client.GetAsync($"/api/clients/{Guid.NewGuid()}", auth.Token);
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var log = await _factory.Db.ErrorLogs
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(e => e.ErrorCode == "CLIENT_NOT_FOUND");

        log.ShouldNotBeNull();
        log!.StatusCode.ShouldBe(404);
        log.Method.ShouldBe("GET");
        log.Path.ShouldStartWith("/api/clients/");
        log.UserId.ShouldBe(auth.UserId);
        log.TraceId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegularManager_CannotReadErrorLogs()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var resp = await _factory.Client.GetAsync("/api/admin/error-logs", manager.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformAdmin_CanListErrorLogs_MostRecentFirst()
    {
        var manager = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var user = await _factory.Db.Users.FindAsync(manager.UserId);
        user!.Role = OnTimeCRM.Domain.Enums.UserRole.Admin;
        await _factory.Db.SaveChangesAsync();
        _factory.Db.ChangeTracker.Clear();
        var adminToken = await TestHelpers.LoginAsync(_factory.Client, manager.Email);

        // Cause a real error so there's at least one row, instead of inserting it directly —
        // this exercises the full middleware → DB path, not just the read side.
        await _factory.Client.GetAsync($"/api/clients/{Guid.NewGuid()}", adminToken);

        var resp = await _factory.Client.GetAsync("/api/admin/error-logs", adminToken);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var page = await resp.Content.ReadFromJsonAsync<OnTimeCRM.Application.Common.PagedResult<ErrorLogDto>>();
        page.ShouldNotBeNull();
        page!.Items.ShouldContain(e => e.ErrorCode == "CLIENT_NOT_FOUND");
    }

    [Fact]
    public async Task FailedSaveChanges_DoesNotPreventTheErrorLogFromBeingWritten()
    {
        // A DbUpdateException (e.g. a unique-constraint violation) leaves failed entities
        // tracked on the same scoped DbContext that ErrorHandlingMiddleware reuses to write
        // the log — if it didn't clear the change tracker first, this save would re-throw
        // instead of producing a log row.
        var auth = await TestHelpers.RegisterManagerAsync(_factory.Client);

        // Registering the same email twice trips the unique index on Users.Email → 409.
        var dupResp = await _factory.Client.PostAsJsonAsync("/api/auth/register-manager", new
        {
            FullName = "Dup",
            Email = auth.Email,
            Password = "Teste123!",
            Phone = "351911111111",
            CompanyName = "Dup Co",
            BrandName = "Dup Stand",
        });
        dupResp.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var log = await _factory.Db.ErrorLogs
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(e => e.StatusCode == 409);
        log.ShouldNotBeNull();
    }
}
