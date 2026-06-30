using OnTime.Application.DTOs.Auth;
using OnTime.Application.DTOs.Users;

namespace OnTime.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> RegisterManagerAsync(RegisterManagerRequest request, CancellationToken ct = default);
    Task<LoginResponseDto> RegisterSalespersonAsync(RegisterSalespersonRequest request, CancellationToken ct = default);
    Task<LoginResponseDto> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<IEnumerable<MembershipDto>> GetMyMembershipsAsync(Guid userId, CancellationToken ct = default);
    Task<LoginResponseDto> SwitchBrandAsync(Guid userId, Guid brandId, CancellationToken ct = default);
}

public interface IUserService
{
    Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default);
    Task<UserDto> UpdateMeAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default);
    Task<IEnumerable<UserListDto>> GetByBrandAsync(Guid brandId, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(Guid userId, Guid brandId, CancellationToken ct = default);
    Task<UserDto> SetActiveAsync(Guid userId, Guid brandId, SetUserActiveRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}
