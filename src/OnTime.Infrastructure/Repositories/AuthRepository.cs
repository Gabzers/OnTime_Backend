using Microsoft.EntityFrameworkCore;
using OnTime.Application.DTOs.Auth;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;
using OnTime.Infrastructure.Persistence;

namespace OnTime.Infrastructure.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db) => _db = db;

    // ── Reads ─────────────────────────────────────────────────────────────

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await _db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<Company?> FindCompanyAsync(Guid id, CancellationToken ct = default) =>
        await _db.Companies.FindAsync(new object[] { id }, ct);

    public async Task<Brand?> FindBrandAsync(Guid id, CancellationToken ct = default) =>
        await _db.Brands.FindAsync(new object[] { id }, ct);

    public async Task<User?> FindByEmailWithNavigationsAsync(
        string email,
        CancellationToken ct = default) =>
        await _db.Users
            .Include(u => u.Brand)
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<IEnumerable<CompanyLookupDto>> GetCompanyListAsync(
        CancellationToken ct = default) =>
        await _db.Companies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyLookupDto(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<IEnumerable<BrandLookupDto>> GetBrandsByCompanyAsync(
        Guid companyId, CancellationToken ct = default) =>
        await _db.Brands
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BrandLookupDto(b.Id, b.Name, b.PrimaryColor))
            .ToListAsync(ct);

    public async Task<User?> FindByIdWithNavigationsAsync(Guid id, CancellationToken ct = default) =>
        await _db.Users
            .Include(u => u.Brand)
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<bool> HasMembershipAsync(Guid userId, Guid brandId, CancellationToken ct = default) =>
        await _db.UserBrandMemberships.AnyAsync(m => m.UserId == userId && m.BrandId == brandId, ct);

    public async Task<IEnumerable<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserBrandMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Brand.Company.Name).ThenBy(m => m.Brand.Name)
            .Select(m => new MembershipDto(m.Brand.CompanyId, m.Brand.Company.Name, m.BrandId, m.Brand.Name))
            .ToListAsync(ct);

    // ── Writes ────────────────────────────────────────────────────────────

    public void AddCompany(Company company) => _db.Companies.Add(company);
    public void AddBrand(Brand brand) => _db.Brands.Add(brand);
    public void AddUser(User user) => _db.Users.Add(user);
    public void AddStage(ClientStage stage) => _db.ClientStages.Add(stage);
    public void AddStageTemplate(StageNotificationTemplate template) => _db.StageNotificationTemplates.Add(template);
    public void AddNotificationPreference(NotificationPreference pref) => _db.NotificationPreferences.Add(pref);
    public void AddPublicProfile(UserPublicProfile profile) => _db.UserPublicProfiles.Add(profile);
    public void AddMembership(UserBrandMembership membership) => _db.UserBrandMemberships.Add(membership);
    public void AddLeadSource(LeadSourceOption option) => _db.LeadSourceOptions.Add(option);
}
