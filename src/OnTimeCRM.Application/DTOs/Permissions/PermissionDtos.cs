using System.ComponentModel.DataAnnotations;

namespace OnTimeCRM.Application.DTOs.Permissions;

public record MenuPermissionDto(
    Guid   Id,
    int    Role,
    string RouteKey,
    bool   CanView,
    bool   CanCreate,
    bool   CanEdit,
    bool   CanDelete
);

public record UpdateMenuPermissionRequest(
    [Required] string RouteKey,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete
);
