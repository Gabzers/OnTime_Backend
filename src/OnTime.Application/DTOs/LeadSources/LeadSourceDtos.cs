using System.ComponentModel.DataAnnotations;

namespace OnTime.Application.DTOs.LeadSources;

public record LeadSourceOptionDto(
    Guid Id,
    int Code,
    string Name,
    bool IsActive
);

public record CreateLeadSourceRequest(
    [Required] string Name
);

public record UpdateLeadSourceRequest(
    [Required] string Name
);

public record SetLeadSourceActiveRequest(
    bool IsActive
);
