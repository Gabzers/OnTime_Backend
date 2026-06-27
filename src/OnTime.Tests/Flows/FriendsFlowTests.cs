using System.Net;
using System.Net.Http.Json;
using Shouldly;
using OnTime.Application.DTOs.Friends;
using OnTime.Tests.Infrastructure;

namespace OnTime.Tests.Flows;

/// <summary>
/// Flow — Friend search (autocomplete by name or email)
/// </summary>
[Collection("Integration")]
public class FriendsFlowTests : IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;

    public FriendsFlowTests(TestWebAppFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Search_MatchesByPartialNameOrEmail_AndExcludesSelf()
    {
        var me = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var other = await TestHelpers.RegisterManagerAsync(_factory.Client, email: "joana.silva@example.com");

        var byName = await _factory.Client.GetFromJsonAsync<List<FriendSearchResultDto>>(
            "/api/friends/search?q=joana", me.Token);
        byName!.ShouldContain(r => r.UserId == other.UserId);
        byName.ShouldNotContain(r => r.UserId == me.UserId);

        var byEmail = await _factory.Client.GetFromJsonAsync<List<FriendSearchResultDto>>(
            "/api/friends/search?q=joana.silva", me.Token);
        byEmail!.ShouldContain(r => r.UserId == other.UserId);
    }

    [Fact]
    public async Task Search_FlagsAlreadyFriendAndPendingRequest()
    {
        var me = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var pendingTarget = await TestHelpers.RegisterManagerAsync(_factory.Client, email: "pending.target@example.com");
        var friendTarget = await TestHelpers.RegisterManagerAsync(_factory.Client, email: "friend.target@example.com");

        var sendResp = await _factory.Client.PostAsJsonAsync(
            "/api/friends/requests", new SendFriendRequestDto(Email: pendingTarget.Email), me.Token);
        sendResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var acceptedReqResp = await _factory.Client.PostAsJsonAsync(
            "/api/friends/requests", new SendFriendRequestDto(UserId: friendTarget.UserId), me.Token);
        var acceptedReq = await acceptedReqResp.Content.ReadFromJsonAsync<FriendRequestDto>();
        var acceptResp = await _factory.Client.PatchAsJsonAsync<object?>(
            $"/api/friends/requests/{acceptedReq!.FriendshipId}/accept", null, friendTarget.Token);
        acceptResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var results = await _factory.Client.GetFromJsonAsync<List<FriendSearchResultDto>>(
            "/api/friends/search?q=target", me.Token);

        var pendingResult = results!.Single(r => r.UserId == pendingTarget.UserId);
        pendingResult.RequestPending.ShouldBeTrue();
        pendingResult.AlreadyFriend.ShouldBeFalse();

        var friendResult = results.Single(r => r.UserId == friendTarget.UserId);
        friendResult.AlreadyFriend.ShouldBeTrue();
    }

    [Fact]
    public async Task Search_MasksEmail_AndRejectsSingleCharacterQuery()
    {
        var me = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var other = await TestHelpers.RegisterManagerAsync(_factory.Client, email: "joana.silva@example.com");

        // A 1-character query against a platform-wide directory is a scraping vector — rejected.
        var tooShort = await _factory.Client.GetFromJsonAsync<List<FriendSearchResultDto>>(
            "/api/friends/search?q=j", me.Token);
        tooShort.ShouldBeEmpty();

        var results = await _factory.Client.GetFromJsonAsync<List<FriendSearchResultDto>>(
            "/api/friends/search?q=joana", me.Token);
        var match = results!.Single(r => r.UserId == other.UserId);
        match.Email.ShouldNotBe(other.Email);
        match.Email.ShouldStartWith("j");
        match.Email.ShouldEndWith("@example.com");
        match.Email.ShouldContain("***");
    }

    [Fact]
    public async Task SendRequest_ByUserId_CreatesPendingRequest()
    {
        var me = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var target = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var resp = await _factory.Client.PostAsJsonAsync(
            "/api/friends/requests", new SendFriendRequestDto(UserId: target.UserId), me.Token);

        resp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<FriendRequestDto>();
        dto!.SenderId.ShouldBe(me.UserId);
    }

    [Fact]
    public async Task SentRequests_AppearForSender_AndCanBeCancelled()
    {
        var me = await TestHelpers.RegisterManagerAsync(_factory.Client);
        var target = await TestHelpers.RegisterManagerAsync(_factory.Client);

        var sendResp = await _factory.Client.PostAsJsonAsync(
            "/api/friends/requests", new SendFriendRequestDto(UserId: target.UserId), me.Token);
        var sent = await sendResp.Content.ReadFromJsonAsync<FriendRequestDto>();

        // The response describes the sender (me), not the receiver.
        sent!.SenderId.ShouldBe(me.UserId);
        sent.SenderEmail.ShouldBe(me.Email, StringCompareShould.IgnoreCase);
        sent.SenderName.ShouldNotBeNullOrEmpty();

        // Sender can see it as a pending sent request.
        var sentList = await _factory.Client.GetFromJsonAsync<List<SentFriendRequestDto>>(
            "/api/friends/requests/sent", me.Token);
        sentList!.ShouldContain(r => r.FriendshipId == sent!.FriendshipId && r.ReceiverId == target.UserId);

        // The full email is never sent over the wire for pending requests either — masked like search results.
        sentList.Single(r => r.FriendshipId == sent!.FriendshipId).ReceiverEmail.ShouldNotBe(target.Email);

        var receiverInbox = await _factory.Client.GetFromJsonAsync<List<FriendRequestDto>>(
            "/api/friends/requests", target.Token);
        receiverInbox!.Single(r => r.FriendshipId == sent!.FriendshipId).SenderEmail.ShouldNotBe(me.Email);

        // The receiver never sent anything — their own "sent" list is empty.
        var targetSentList = await _factory.Client.GetFromJsonAsync<List<SentFriendRequestDto>>(
            "/api/friends/requests/sent", target.Token);
        targetSentList.ShouldBeEmpty();

        // Only the sender can cancel; the receiver gets 403.
        var receiverCancelResp = await _factory.Client.DeleteAsync(
            $"/api/friends/requests/{sent!.FriendshipId}", target.Token);
        ((int)receiverCancelResp.StatusCode).ShouldBe(403);

        var cancelResp = await _factory.Client.DeleteAsync(
            $"/api/friends/requests/{sent.FriendshipId}", me.Token);
        cancelResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Gone from the sender's list, and the receiver no longer has a pending request either.
        var afterCancel = await _factory.Client.GetFromJsonAsync<List<SentFriendRequestDto>>(
            "/api/friends/requests/sent", me.Token);
        afterCancel!.ShouldNotContain(r => r.FriendshipId == sent.FriendshipId);

        var receiverRequests = await _factory.Client.GetFromJsonAsync<List<FriendRequestDto>>(
            "/api/friends/requests", target.Token);
        receiverRequests!.ShouldNotContain(r => r.FriendshipId == sent.FriendshipId);
    }
}
