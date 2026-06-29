using Microsoft.EntityFrameworkCore;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;
using OnTime.Infrastructure.Persistence;

namespace OnTime.Infrastructure.Repositories;

public sealed class LeadSourceRepository : ILeadSourceRepository
{
    private readonly AppDbContext _db;

    public LeadSourceRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<LeadSourceOption>> GetByCompanyAsync(
        Guid companyId, CancellationToken ct = default) =>
        await _db.LeadSourceOptions
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

    public async Task<LeadSourceOption?> FindAsync(Guid id, CancellationToken ct = default) =>
        await _db.LeadSourceOptions.FindAsync(new object[] { id }, ct);

    public async Task<int> GetNextCodeAsync(Guid companyId, CancellationToken ct = default)
    {
        var max = await _db.LeadSourceOptions
            .Where(x => x.CompanyId == companyId)
            .Select(x => (int?)x.Code)
            .MaxAsync(ct);
        return (max ?? -1) + 1;
    }

    public void Add(LeadSourceOption option) => _db.LeadSourceOptions.Add(option);
}
