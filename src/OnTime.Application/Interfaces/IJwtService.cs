using OnTime.Application.DTOs.Users;

namespace OnTime.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(UserDetailRow user);
    DateTimeOffset GetExpiry();
}
