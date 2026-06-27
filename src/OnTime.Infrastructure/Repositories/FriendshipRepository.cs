using Microsoft.EntityFrameworkCore;
using OnTime.Application.DTOs.Friends;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;
using OnTime.Domain.Enums;
using OnTime.Infrastructure.Persistence;

namespace OnTime.Infrastructure.Repositories;

public sealed class FriendshipRepository : IFriendshipRepository
{
    private readonly AppDbContext _db;

    public FriendshipRepository(AppDbContext db) => _db = db;

    public async Task<UserFriendship?> FindAsync(
        Guid senderId, Guid receiverId, CancellationToken ct = default) =>
        await _db.UserFriendships
            .FirstOrDefaultAsync(f =>
                (f.SenderId == senderId && f.ReceiverId == receiverId) ||
                (f.SenderId == receiverId && f.ReceiverId == senderId), ct);

    public async Task<UserFriendship?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.UserFriendships.FindAsync(new object[] { id }, ct);

    public async Task<IEnumerable<FriendDto>> GetAcceptedFriendsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.UserFriendships
            .AsNoTracking()
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f =>
                f.Status == FriendshipStatus.Accepted &&
                (f.SenderId == userId || f.ReceiverId == userId))
            .ToListAsync(ct);

        return rows.Select(f =>
        {
            var friend = f.SenderId == userId ? f.Receiver : f.Sender;
            return new FriendDto(f.Id, friend.Id, friend.FullName, null);
        });
    }

    public async Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.UserFriendships
            .AsNoTracking()
            .Include(f => f.Sender)
            .Where(f => f.ReceiverId == userId && f.Status == FriendshipStatus.Pending)
            .ToListAsync(ct);

        return rows.Select(f =>
            new FriendRequestDto(f.Id, f.Sender.Id, f.Sender.FullName, MaskEmail(f.Sender.Email), f.CreatedAt));
    }

    public async Task<IEnumerable<SentFriendRequestDto>> GetSentRequestsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.UserFriendships
            .AsNoTracking()
            .Include(f => f.Receiver)
            .Where(f => f.SenderId == userId && f.Status == FriendshipStatus.Pending)
            .ToListAsync(ct);

        return rows.Select(f =>
            new SentFriendRequestDto(f.Id, f.Receiver.Id, f.Receiver.FullName, MaskEmail(f.Receiver.Email), f.CreatedAt));
    }

    public async Task<User?> FindUserByEmailAsync(
        string email, CancellationToken ct = default) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

    public async Task<User?> FindUserByIdAsync(
        Guid id, CancellationToken ct = default) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);

    public async Task<IEnumerable<FriendSearchResultDto>> SearchUsersAsync(
        Guid userId, string query, CancellationToken ct = default)
    {
        // A 1-character query against a global, cross-tenant user directory is a scraping
        // vector (an attacker could enumerate every user a couple of letters at a time).
        var term = query.Trim().ToLower();
        if (term.Length < 2) return [];

        var matches = await _db.Users
            .AsNoTracking()
            .Include(u => u.Brand)
            .Include(u => u.Company)
            .Include(u => u.PublicProfile)
            .Where(u => u.IsActive && u.Id != userId &&
                (u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term)))
            .OrderBy(u => u.FullName)
            .Take(10)
            .ToListAsync(ct);

        if (matches.Count == 0) return [];

        var matchIds = matches.Select(u => u.Id).ToList();
        var friendships = await _db.UserFriendships
            .AsNoTracking()
            .Where(f =>
                (f.SenderId == userId && matchIds.Contains(f.ReceiverId)) ||
                (f.ReceiverId == userId && matchIds.Contains(f.SenderId)))
            .ToListAsync(ct);

        return matches.Select(u =>
        {
            var fr = friendships.FirstOrDefault(f => f.SenderId == u.Id || f.ReceiverId == u.Id);
            return new FriendSearchResultDto(
                u.Id,
                u.FullName,
                MaskEmail(u.Email),
                u.PublicProfile?.AvatarUrl,
                u.Brand?.Name,
                u.Company?.Name,
                AlreadyFriend: fr?.Status == FriendshipStatus.Accepted,
                RequestPending: fr?.Status == FriendshipStatus.Pending
            );
        });
    }

    /// <summary>
    /// "j***@stand.pt" — search results are shown to users who aren't friends yet, so the
    /// full address (a scraping target across the whole platform) is never sent over the wire.
    /// </summary>
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        return $"{email[0]}***{email[at..]}";
    }

    public async Task<FriendProfileDto?> GetFriendProfileAsync(
        Guid viewerUserId, Guid friendUserId, CancellationToken ct = default)
    {
        // Verify they are actually friends
        var areFriends = await _db.UserFriendships.AnyAsync(f =>
            f.Status == FriendshipStatus.Accepted &&
            ((f.SenderId == viewerUserId && f.ReceiverId == friendUserId) ||
             (f.SenderId == friendUserId && f.ReceiverId == viewerUserId)), ct);

        if (!areFriends) return null;

        var friend = await _db.Users
            .AsNoTracking()
            .Include(u => u.PublicProfile)
            .FirstOrDefaultAsync(u => u.Id == friendUserId, ct);

        if (friend is null) return null;

        var profile = friend.PublicProfile;

        // Compute KPIs only for the fields marked as public
        int? salesCount      = null;
        int? proposalsCount  = null;
        int? hotDealsCount   = null;
        decimal? avgSaleValue = null;
        decimal? conversionRate = null;

        if (profile is not null)
        {
            if (profile.ShowSalesCount)
                salesCount = await _db.Sales.CountAsync(s => s.UserId == friendUserId, ct);

            if (profile.ShowProposalsCount)
                proposalsCount = await _db.Proposals.CountAsync(p => p.UserId == friendUserId, ct);

            if (profile.ShowHotDealsCount)
                hotDealsCount = await _db.Clients.CountAsync(c =>
                    c.UserId == friendUserId &&
                    c.IsActive &&
                    c.Temperature == DealTemperature.Hot, ct);

            if (profile.ShowAvgSaleValue)
            {
                var avg = await _db.Sales
                    .Where(s => s.UserId == friendUserId)
                    .AverageAsync(s => (decimal?)s.FinalValue, ct);
                avgSaleValue = avg;
            }

            if (profile.ShowConversionRate && proposalsCount.HasValue && salesCount.HasValue &&
                proposalsCount.Value > 0)
            {
                conversionRate = (decimal)salesCount.Value / proposalsCount.Value * 100m;
            }
        }

        return new FriendProfileDto(
            friend.Id,
            friend.FullName,
            profile?.AvatarUrl,
            salesCount,
            proposalsCount,
            conversionRate,
            hotDealsCount,
            avgSaleValue);
    }

    public async Task<UserPublicProfile?> FindPublicProfileAsync(
        Guid userId, CancellationToken ct = default) =>
        await _db.UserPublicProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public void Add(UserFriendship friendship)        => _db.UserFriendships.Add(friendship);
    public void Remove(UserFriendship friendship)     => _db.UserFriendships.Remove(friendship);
    public void AddPublicProfile(UserPublicProfile p) => _db.UserPublicProfiles.Add(p);
}
