using Microsoft.EntityFrameworkCore;
using OnTimeCRM.Application.DTOs.Users;
using OnTimeCRM.Application.Interfaces.Repositories;
using OnTimeCRM.Domain.Entities;
using OnTimeCRM.Infrastructure.Persistence;

namespace OnTimeCRM.Infrastructure.Repositories;

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

    public async Task<IEnumerable<Guid>> GetVehicleBrandIdsAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserVehicleBrands
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.VehicleBrandId)
            .ToListAsync(ct);

    public async Task SetVehicleBrandIdsAsync(
        Guid userId, IEnumerable<Guid> brandIds, CancellationToken ct = default)
    {
        var existing = await _db.UserVehicleBrands
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
        _db.UserVehicleBrands.RemoveRange(existing);

        foreach (var brandId in brandIds.Distinct())
            _db.UserVehicleBrands.Add(new UserVehicleBrand { UserId = userId, VehicleBrandId = brandId });
    }
}
