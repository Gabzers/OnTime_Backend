using OnTime.Application.DTOs.Notifications;
using OnTime.Domain.Entities;

namespace OnTime.Application.Interfaces.Repositories;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreferenceDto?> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationPreference?> FindByUserAsync(Guid userId, CancellationToken ct = default);
}
