using Microsoft.EntityFrameworkCore;
using OnTime.Application.DTOs.Companies;
using OnTime.Application.DTOs.Users;
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

    public async Task<IEnumerable<UserListDto>> GetUsersByCompanyAsync(
        Guid companyId, CancellationToken ct = default) =>
        await _db.Users
            .AsNoTracking()
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.FullName)
            .Select(u => new UserListDto(
                u.Id,
                u.FullName,
                u.Email,
                u.Phone,
                (int)u.Role,
                (int)u.AccountStatus,
                u.CreatedAt))
            .ToListAsync(ct);

    public async Task<User?> FindUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Users.FindAsync(new object[] { userId }, ct);

    public async Task<Brand?> FindBrandAsync(Guid brandId, CancellationToken ct = default) =>
        await _db.Brands.FindAsync(new object[] { brandId }, ct);

    public async Task GrantMembershipAsync(Guid userId, Guid brandId, CancellationToken ct = default)
    {
        var exists = await _db.UserBrandMemberships
            .AnyAsync(m => m.UserId == userId && m.BrandId == brandId, ct);
        if (!exists)
            _db.UserBrandMemberships.Add(new UserBrandMembership { UserId = userId, BrandId = brandId });
    }

    public async Task RevokeMembershipAsync(Guid userId, Guid brandId, CancellationToken ct = default)
    {
        var membership = await _db.UserBrandMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.BrandId == brandId, ct);
        if (membership is not null)
            _db.UserBrandMemberships.Remove(membership);
    }
}
