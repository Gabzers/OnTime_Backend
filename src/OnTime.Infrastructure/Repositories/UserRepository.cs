using Microsoft.EntityFrameworkCore;
using OnTime.Application.DTOs.Users;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;
using OnTime.Infrastructure.Persistence;

namespace OnTime.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> FindAsync(Guid id, CancellationToken ct = default) =>
        await _db.Users.FindAsync(new object[] { id }, ct);

    public async Task<User?> FindWithBrandAndCompanyAsync(Guid id, CancellationToken ct = default) =>
        await _db.Users
            .Include(u => u.Company)
            .Include(u => u.Brand)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IEnumerable<UserListDto>> GetByBrandAsync(
        Guid brandId,
        CancellationToken ct = default) =>
        await _db.Users
            .AsNoTracking()
            .Where(u => u.BrandId == brandId)
            .OrderBy(u => u.FullName)
            .Select(u => new UserListDto(
                u.Id, u.FullName, u.Email, u.Phone,
                (int)u.Role, (int)u.AccountStatus, u.CreatedAt))
            .ToListAsync(ct);

    public async Task<User?> FindInBrandAsync(
        Guid userId,
        Guid brandId,
        CancellationToken ct = default) =>
        await _db.Users
            .Include(u => u.Company)
            .Include(u => u.Brand)
            .FirstOrDefaultAsync(u => u.Id == userId && u.BrandId == brandId, ct);

    public async Task<bool> IsAutomotiveAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Brand == null || u.Brand.IsAutomotive)
            .FirstOrDefaultAsync(ct);

    public async Task<bool> EmailTakenByAnotherUserAsync(
        string email, Guid excludingUserId, CancellationToken ct = default) =>
        await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email && u.Id != excludingUserId, ct);
}
