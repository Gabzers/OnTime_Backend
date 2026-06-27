using OnTime.Application.DTOs.Permissions;

namespace OnTime.Application.Interfaces;

public interface IPermissionService
{
    Task<IEnumerable<MenuPermissionDto>> GetPermissionsAsync(int role, CancellationToken ct = default);
    Task UpdatePermissionsAsync(int role, IEnumerable<UpdateMenuPermissionRequest> updates, CancellationToken ct = default);
}
