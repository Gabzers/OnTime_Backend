using Microsoft.EntityFrameworkCore;
using OnTime.Application.Common;
using OnTime.Application.DTOs.Subscription;
using OnTime.Application.Interfaces;
using OnTime.Domain.Enums;

namespace OnTime.Application.Services;

/// <summary>
/// Stub implementation — payment integration (Stripe/IfthenPay) is post-MVP.
/// Returns sensible defaults so the UI can render without 500 errors.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly IAppDbContext _db;

    public SubscriptionService(IAppDbContext db) => _db = db;

    public async Task<SubscriptionStatusDto> GetStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.AccountStatus,
                u.SubscriptionStatus,
                u.Plan,
                u.TrialEndsAt,
                u.SubscriptionExpiresAt
            })
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        var accountStatus      = user?.AccountStatus      ?? UserAccountStatus.Active;
        var subscriptionStatus = user?.SubscriptionStatus ?? SubscriptionStatus.Trial;
        var plan               = user?.Plan               ?? SubscriptionPlan.Trial;
        var trialEndsAt        = user?.TrialEndsAt;
        var expiresAt          = user?.SubscriptionExpiresAt;

        var now           = DateTimeOffset.UtcNow;
        var isTrialActive = accountStatus == UserAccountStatus.PendingActivation
                            && trialEndsAt.HasValue
                            && trialEndsAt.Value > now;

        int? daysUntilExpiry = expiresAt.HasValue
            ? (int)Math.Max(0, Math.Ceiling((expiresAt.Value - now).TotalDays))
            : isTrialActive && trialEndsAt.HasValue
                ? (int)Math.Max(0, Math.Ceiling((trialEndsAt.Value - now).TotalDays))
                : null;

        return new SubscriptionStatusDto(
            AccountStatus:      (int)accountStatus,
            Plan:               (int)plan,
            SubscriptionStatus: (int)subscriptionStatus,
            TrialEndsAt:        trialEndsAt,
            ExpiresAt:          expiresAt,
            IsTrialActive:      isTrialActive,
            DaysUntilExpiry:    daysUntilExpiry,
            IsExpired:          accountStatus == UserAccountStatus.Expired,
            CanRenew:           accountStatus is UserAccountStatus.Expired or UserAccountStatus.Cancelled
        );
    }

    public Task<IEnumerable<SubscriptionPaymentDto>> GetPaymentsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Enumerable.Empty<SubscriptionPaymentDto>());

    public Task<InitiateSubscriptionResponseDto> InitiateAsync(
        Guid userId,
        InitiateSubscriptionRequest request,
        CancellationToken ct = default)
        => throw new ApiException(ApiErrorCatalog.PAYMENT_PENDING);

    public Task<SubscriptionPaymentDto> GetPaymentStatusAsync(Guid paymentId, Guid userId, CancellationToken ct = default)
        => throw new ApiException(ApiErrorCatalog.PAYMENT_NOT_FOUND);

    public Task CancelAsync(Guid userId, CancellationToken ct = default)
        => Task.CompletedTask;
}
