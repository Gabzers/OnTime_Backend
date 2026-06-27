using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OnTime.Application.DTOs.Users;
using OnTime.Application.Interfaces;

namespace OnTime.Infrastructure.Security;

public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtService(IConfiguration configuration)
    {
        _key               = configuration["Jwt:Key"]              ?? throw new InvalidOperationException("Jwt:Key is not configured");
        _issuer            = configuration["Jwt:Issuer"]           ?? "OnTime";
        _audience          = configuration["Jwt:Audience"]         ?? "OnTime-Frontend";
        _expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "480");
    }

    public string GenerateToken(UserDetailRow user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim("role",                    user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.CompanyId.HasValue)
            claims.Add(new Claim("cid", user.CompanyId.Value.ToString()));

        if (user.BrandId.HasValue)
            claims.Add(new Claim("bid", user.BrandId.Value.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds      = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTimeOffset GetExpiry() =>
        DateTimeOffset.UtcNow.AddMinutes(_expirationMinutes);
}
