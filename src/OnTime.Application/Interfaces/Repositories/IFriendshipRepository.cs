using OnTime.Application.DTOs.Friends;
using OnTime.Domain.Entities;
using OnTime.Domain.Enums;

namespace OnTime.Application.Interfaces.Repositories;

public interface IFriendshipRepository
{
    Task<UserFriendship?> FindAsync(Guid senderId, Guid receiverId, CancellationToken ct = default);
    Task<UserFriendship?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<FriendDto>> GetAcceptedFriendsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<SentFriendRequestDto>> GetSentRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<User?> FindUserByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<FriendSearchResultDto>> SearchUsersAsync(Guid userId, string query, CancellationToken ct = default);
    Task<FriendProfileDto?> GetFriendProfileAsync(Guid viewerUserId, Guid friendUserId, CancellationToken ct = default);
    Task<UserPublicProfile?> FindPublicProfileAsync(Guid userId, CancellationToken ct = default);

    void Add(UserFriendship friendship);
    void Remove(UserFriendship friendship);
    void AddPublicProfile(UserPublicProfile profile);
}
