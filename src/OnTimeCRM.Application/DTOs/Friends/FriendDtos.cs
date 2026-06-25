namespace OnTimeCRM.Application.DTOs.Friends;

// ── Friend list ───────────────────────────────────────────────────────────────
public record FriendDto(
    Guid FriendshipId,
    Guid UserId,
    string FullName,
    string? AvatarUrl
);

// ── Pending request ───────────────────────────────────────────────────────────
public record FriendRequestDto(
    Guid FriendshipId,
    Guid SenderId,
    string SenderName,
    string SenderEmail,
    DateTimeOffset SentAt
);

// ── Public KPI profile (fields gated by the friend's privacy settings) ────────
public record FriendProfileDto(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    int? SalesCount,
    int? ProposalsCount,
    decimal? ConversionRate,
    int? HotDealsCount,
    decimal? AvgSaleValue
);

// ── Requests ──────────────────────────────────────────────────────────────────
public record SendFriendRequestDto(string? Email = null, Guid? UserId = null);

// ── Sent request (still pending, from the sender's point of view) ────────────
public record SentFriendRequestDto(
    Guid FriendshipId,
    Guid ReceiverId,
    string ReceiverName,
    string ReceiverEmail,
    DateTimeOffset SentAt
);

// ── Search (autocomplete by name or email) ───────────────────────────────────
public record FriendSearchResultDto(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? BrandName,
    string? CompanyName,
    bool AlreadyFriend,
    bool RequestPending
);

// ── Public profile settings ──────────────────────────────────────────────────
public record PublicProfileSettingsDto(
    bool ShowSalesCount,
    bool ShowConversionRate,
    bool ShowProposalsCount,
    bool ShowHotDealsCount,
    bool ShowAvgSaleValue,
    string? AvatarUrl
);
