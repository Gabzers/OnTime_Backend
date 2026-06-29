using OnTime.Application.Common;
using OnTime.Application.DTOs.Vehicles;
using OnTime.Domain.Entities;

namespace OnTime.Application.Interfaces.Repositories;

public interface IVehicleRepository
{
    Task<IEnumerable<VehicleBrandDto>> GetBrandsAsync(CancellationToken ct = default);
    Task<PagedResult<VehicleModelListDto>> GetModelsAsync(VehicleSearchParams p, Guid userId, CancellationToken ct = default);
    Task<VehicleModelDto?> GetModelDtoAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<VehicleBrand?> FindBrandAsync(Guid id, CancellationToken ct = default);
    Task<UserVehicleModel?> FindModelAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<bool> IsModelInUseAsync(Guid modelId, CancellationToken ct = default);
    Task<bool> IsVersionInUseAsync(Guid versionId, CancellationToken ct = default);
    Task<bool> IsBrandAllowedForUserAsync(Guid userId, Guid vehicleBrandId, CancellationToken ct = default);

    void AddBrand(VehicleBrand brand);
    void RemoveBrand(VehicleBrand brand);
    void AddModel(UserVehicleModel model);
    void RemoveModel(UserVehicleModel model);

    // Versions
    Task<IEnumerable<VehicleVersionDto>> GetVersionsAsync(Guid modelId, Guid userId, CancellationToken ct = default);
    Task<UserVehicleVersion?> FindVersionAsync(Guid id, Guid userId, CancellationToken ct = default);
    void AddVersion(UserVehicleVersion version);
    void RemoveVersion(UserVehicleVersion version);
}
