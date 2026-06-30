using OnTime.Domain.Entities;

namespace OnTime.Application.Interfaces.Repositories;

public interface ILeadSourceRepository
{
    Task<IEnumerable<LeadSourceOption>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<LeadSourceOption?> FindAsync(Guid id, CancellationToken ct = default);
    Task<int> GetNextCodeAsync(Guid companyId, CancellationToken ct = default);
    void Add(LeadSourceOption option);
}
