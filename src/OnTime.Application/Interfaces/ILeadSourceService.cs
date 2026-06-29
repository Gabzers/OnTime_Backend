using OnTime.Application.DTOs.LeadSources;

namespace OnTime.Application.Interfaces;

public interface ILeadSourceService
{
    Task<IEnumerable<LeadSourceOptionDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<LeadSourceOptionDto> CreateAsync(Guid companyId, CreateLeadSourceRequest req, CancellationToken ct = default);
    Task<LeadSourceOptionDto> UpdateAsync(Guid id, Guid companyId, UpdateLeadSourceRequest req, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, Guid companyId, bool isActive, CancellationToken ct = default);
}
