using OnTimeCRM.Application.DTOs.Users;
using OnTimeCRM.Domain.Entities;

namespace OnTimeCRM.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> FindAsync(Guid id, CancellationToken ct = default);
    Task<User?> FindWithBrandAndCompanyAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<UserListDto>> GetByBrandAsync(Guid brandId, CancellationToken ct = default);
    Task<User?> FindInBrandAsync(Guid userId, Guid brandId, CancellationToken ct = default);

    Task<IEnumerable<Guid>> GetVehicleBrandIdsAsync(Guid userId, CancellationToken ct = default);
    Task SetVehicleBrandIdsAsync(Guid userId, IEnumerable<Guid> brandIds, CancellationToken ct = default);
}
