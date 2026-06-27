using Microsoft.EntityFrameworkCore;
using OnTime.Application.DTOs.Companies;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;
using OnTime.Infrastructure.Persistence;

namespace OnTime.Infrastructure.Repositories;

public sealed class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _db;

    public AdminRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<CompanyAdminDto>> GetCompaniesAsync(
        CancellationToken ct = default) =>
        await _db.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CompanyAdminDto(
                c.Id,
                c.Name,
                c.Phone,
                c.Email,
                c.Address,
                c.IsActive,
                c.Brands.Count,
                c.Users.Count,
                c.CreatedAt))
            .ToListAsync(ct);

    public async Task<Company?> FindCompanyAsync(Guid id, CancellationToken ct = default) =>
        await _db.Companies.FindAsync(new object[] { id }, ct);

    public void AddCompany(Company company) => _db.Companies.Add(company);
}
