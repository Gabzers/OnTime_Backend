using System.Security.Claims;

namespace OnTime.Application.Common;

/// <summary>
/// Derived scope for the current request. Single source of truth for all
/// ownership/visibility decisions — no more ad-hoc ?? Guid.Empty.
/// </summary>
public readonly record struct AccessScope(Guid UserId, Guid? BrandId, Guid? CompanyId, int Role)
{
    public bool IsAdmin          => Role == 2;
    public bool IsManagerOrAdmin => Role >= 1;

    /// <summary>
    /// Returns the brand filter to apply for list queries.
    /// Manager/Admin → their BrandId (brand-wide); Salesperson → null (own-only).
    /// Throws AUTH_FORBIDDEN when the required bid claim is missing.
    /// </summary>
    public Guid? ManagerBrandScope
    {
        get
        {
            if (!IsManagerOrAdmin) return null;
            return BrandId ?? throw new ApiException(ApiErrorCatalog.AUTH_FORBIDDEN);
        }
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("sub claim missing"));

    public static Guid? TryGetCompanyId(this ClaimsPrincipal principal)
    {
        var val = principal.FindFirst("cid")?.Value;
        return val is null ? null : Guid.Parse(val);
    }

    public static Guid GetCompanyId(this ClaimsPrincipal principal) =>
        TryGetCompanyId(principal)
            ?? throw new InvalidOperationException("cid claim missing");

    public static Guid? TryGetBrandId(this ClaimsPrincipal principal)
    {
        var val = principal.FindFirst("bid")?.Value;
        return val is null ? null : Guid.Parse(val);
    }

    public static Guid GetBrandId(this ClaimsPrincipal principal) =>
        TryGetBrandId(principal)
            ?? throw new InvalidOperationException("bid claim missing");

    public static int GetRole(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new InvalidOperationException("role claim missing"));

    public static bool IsManager(this ClaimsPrincipal principal)      => principal.GetRole() == 1;
    public static bool IsAdmin(this ClaimsPrincipal principal)         => principal.GetRole() == 2;
    public static bool IsManagerOrAdmin(this ClaimsPrincipal principal) => principal.GetRole() >= 1;

    /// <summary>
    /// Builds an AccessScope from the current principal's claims.
    /// Use this instead of reading claims ad-hoc in controllers.
    /// </summary>
    public static AccessScope Scope(this ClaimsPrincipal principal) => new(
        UserId:    principal.GetUserId(),
        BrandId:   principal.TryGetBrandId(),
        CompanyId: principal.TryGetCompanyId(),
        Role:      principal.GetRole()
    );

    /// <summary>
    /// Returns companyId from the claim. Throws AUTH_FORBIDDEN if claim is missing.
    /// </summary>
    public static Guid RequireCompanyId(this ClaimsPrincipal principal) =>
        principal.TryGetCompanyId() ?? throw new ApiException(ApiErrorCatalog.AUTH_FORBIDDEN);

    /// <summary>
    /// Returns brandId from the claim. Throws AUTH_FORBIDDEN if claim is missing.
    /// </summary>
    public static Guid RequireBrandId(this ClaimsPrincipal principal) =>
        principal.TryGetBrandId() ?? throw new ApiException(ApiErrorCatalog.AUTH_FORBIDDEN);
}
