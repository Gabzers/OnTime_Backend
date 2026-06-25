using Microsoft.EntityFrameworkCore;
using OnTimeCRM.Application.Common;
using OnTimeCRM.Application.DTOs.Vehicles;
using OnTimeCRM.Application.Interfaces.Repositories;
using OnTimeCRM.Domain.Entities;
using OnTimeCRM.Infrastructure.Persistence;

namespace OnTimeCRM.Infrastructure.Repositories;

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _db;

    public VehicleRepository(AppDbContext db) => _db = db;

    // ── Reads ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<VehicleBrandDto>> GetBrandsAsync(CancellationToken ct = default) =>
        await _db.VehicleBrands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new VehicleBrandDto(b.Id, b.Name, b.LogoUrl, b.Models.Count()))
            .ToListAsync(ct);

    public async Task<PagedResult<VehicleModelListDto>> GetModelsAsync(
        VehicleSearchParams p,
        Guid userId,
        CancellationToken ct = default)
    {
        var query = _db.VehicleModels
            .AsNoTracking()
            .Include(m => m.Brand)
            .AsQueryable();

        if (p.BrandId.HasValue)
        {
            query = query.Where(m => m.BrandId == p.BrandId.Value);
        }
        else
        {
            // No explicit brand filter — default to the user's selected brands.
            // Empty selection = no filter = all brands (see USER-BRANDS.md).
            var selectedBrandIds = await _db.UserVehicleBrands
                .Where(x => x.UserId == userId)
                .Select(x => x.VehicleBrandId)
                .ToListAsync(ct);

            if (selectedBrandIds.Count > 0)
                query = query.Where(m => selectedBrandIds.Contains(m.BrandId));
        }

        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var s = p.Search.ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(s) ||
                m.Brand.Name.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var size  = Math.Clamp(p.PageSize, 1, 50);

        var items = await query
            .OrderBy(m => m.Brand.Name).ThenBy(m => m.Name)
            .Skip((p.Page - 1) * size)
            .Take(size)
            .Select(m => new VehicleModelListDto(
                m.Id, m.BrandId, m.Brand.Name, m.Name,
                m.Version, m.Year, m.FuelType == null ? null : (int)m.FuelType,
                m.IsActive,
                m.Versions.Any(v => v.ExternalColors != null && v.ExternalColors != "" && v.ExternalColors != "[]")))
            .ToListAsync(ct);

        return new PagedResult<VehicleModelListDto>(items, total, p.Page, size);
    }

    public async Task<VehicleModelDto?> GetModelDtoAsync(Guid id, CancellationToken ct = default)
    {
        var m = await _db.VehicleModels
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.Versions)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return m is null ? null : ToModelDto(m);
    }

    public async Task<VehicleBrand?> FindBrandAsync(Guid id, CancellationToken ct = default) =>
        await _db.VehicleBrands.FindAsync(new object[] { id }, ct);

    public async Task<VehicleModel?> FindModelAsync(Guid id, CancellationToken ct = default) =>
        await _db.VehicleModels
            .Include(m => m.Brand)
            .Include(m => m.Versions)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    // ── Version Reads ──────────────────────────────────────────────────────

    public async Task<IEnumerable<VehicleVersionDto>> GetVersionsAsync(Guid modelId, CancellationToken ct = default) =>
        await _db.VehicleModelVersions
            .AsNoTracking()
            .Where(v => v.ModelId == modelId)
            .OrderBy(v => v.Name)
            .Select(v => new VehicleVersionDto(
                v.Id, v.Name,
                ColorArrayHelper.Parse(v.ExternalColors),
                ColorArrayHelper.Parse(v.InternalColors)))
            .ToListAsync(ct);

    public async Task<VehicleModelVersion?> FindVersionAsync(Guid id, CancellationToken ct = default) =>
        await _db.VehicleModelVersions.FindAsync(new object[] { id }, ct);

    // ── Writes ────────────────────────────────────────────────────────────

    public void AddBrand(VehicleBrand brand) => _db.VehicleBrands.Add(brand);
    public void RemoveBrand(VehicleBrand brand) => _db.VehicleBrands.Remove(brand);
    public void AddModel(VehicleModel model) => _db.VehicleModels.Add(model);
    public void RemoveModel(VehicleModel model) => _db.VehicleModels.Remove(model);
    public void AddVersion(VehicleModelVersion version) => _db.VehicleModelVersions.Add(version);
    public void RemoveVersion(VehicleModelVersion version) => _db.VehicleModelVersions.Remove(version);

    // ── Mapper ────────────────────────────────────────────────────────────

    private static VehicleModelDto ToModelDto(VehicleModel m) =>
        new(m.Id, m.BrandId, m.Brand.Name, m.Name, m.Version, m.Year,
            m.FuelType is null ? null : (int)m.FuelType,
            m.BasePrice, m.ImageUrl,
            m.Versions
                .OrderBy(v => v.Name)
                .Select(v => new VehicleVersionDto(
                    v.Id, v.Name,
                    ColorArrayHelper.Parse(v.ExternalColors),
                    ColorArrayHelper.Parse(v.InternalColors)))
                .ToList(),
            m.IsActive);
}
