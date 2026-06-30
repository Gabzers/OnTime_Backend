using OnTime.Application.Common;
using OnTime.Application.DTOs.Brands;
using OnTime.Application.Interfaces;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;

namespace OnTime.Application.Services;

public class BrandService : IBrandService
{
    private readonly IBrandRepository _repo;
    private readonly IAuthRepository  _authRepo;
    private readonly IUserRepository  _userRepo;
    private readonly IUnitOfWork      _uow;

    public BrandService(IBrandRepository repo, IAuthRepository authRepo, IUserRepository userRepo, IUnitOfWork uow)
    {
        _repo     = repo;
        _authRepo = authRepo;
        _userRepo = userRepo;
        _uow      = uow;
    }

    public Task<IEnumerable<BrandListDto>> GetByCompanyAsync(
        Guid companyId, CancellationToken ct = default) =>
        _repo.GetByCompanyAsync(companyId, ct);

    public async Task<BrandDto> GetByIdAsync(
        Guid id, Guid companyId, CancellationToken ct = default)
    {
        return await _repo.GetDtoByIdAsync(id, companyId, ct)
            ?? throw new ApiException(ApiErrorCatalog.BRAND_NOT_FOUND);
    }

    public async Task<BrandDto> CreateAsync(
        Guid companyId, CreateBrandRequest req, CancellationToken ct = default)
    {
        var company = await _authRepo.FindCompanyAsync(companyId, ct)
            ?? throw new ApiException(ApiErrorCatalog.COMPANY_NOT_FOUND);

        if (!company.IsActive)
            throw new ApiException(ApiErrorCatalog.COMPANY_INACTIVE);

        var brand = new Brand
        {
            CompanyId    = companyId,
            Name         = req.Name,
            Description  = req.Description,
            Phone        = req.Phone,
            Email        = req.Email,
            Address      = req.Address,
            LogoUrl      = req.LogoUrl,
            PrimaryColor = req.PrimaryColor ?? "#1677FF",
            IsActive     = true,
            IsAutomotive = req.IsAutomotive
        };

        _repo.Add(brand);
        await _uow.SaveChangesAsync(ct);
        return ToDto(brand);
    }

    public async Task<BrandDto> UpdateAsync(
        Guid id, Guid companyId, UpdateBrandRequest req, CancellationToken ct = default)
    {
        var brand = await _repo.FindAsync(id, ct)
            ?? throw new ApiException(ApiErrorCatalog.BRAND_NOT_FOUND);

        if (brand.CompanyId != companyId)
            throw new ApiException(ApiErrorCatalog.BRAND_WRONG_COMPANY);

        brand.Name         = req.Name;
        brand.Description  = req.Description;
        brand.Phone        = req.Phone;
        brand.Email        = req.Email;
        brand.Address      = req.Address;
        brand.LogoUrl      = req.LogoUrl;
        brand.PrimaryColor = req.PrimaryColor ?? brand.PrimaryColor;
        brand.IsAutomotive = req.IsAutomotive;

        await _uow.SaveChangesAsync(ct);
        return ToDto(brand);
    }

    public async Task SetActiveAsync(
        Guid id, Guid companyId, bool isActive, CancellationToken ct = default)
    {
        var brand = await _repo.FindAsync(id, ct)
            ?? throw new ApiException(ApiErrorCatalog.BRAND_NOT_FOUND);

        if (brand.CompanyId != companyId)
            throw new ApiException(ApiErrorCatalog.BRAND_WRONG_COMPANY);

        brand.IsActive = isActive;
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<Brand> RequireOwnedBrandAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var brand = await _repo.FindAsync(id, ct)
            ?? throw new ApiException(ApiErrorCatalog.BRAND_NOT_FOUND);
        if (brand.CompanyId != companyId)
            throw new ApiException(ApiErrorCatalog.BRAND_WRONG_COMPANY);
        return brand;
    }

    public async Task<BrandVehicleBrandsDto> GetVehicleBrandIdsAsync(
        Guid brandId, Guid companyId, CancellationToken ct = default)
    {
        await RequireOwnedBrandAsync(brandId, companyId, ct);
        return new(await _repo.GetVehicleBrandIdsAsync(brandId, ct));
    }

    public async Task SetVehicleBrandIdsAsync(
        Guid brandId, Guid companyId, UpdateBrandVehicleBrandsRequest req, CancellationToken ct = default)
    {
        await RequireOwnedBrandAsync(brandId, companyId, ct);
        await _repo.SetVehicleBrandIdsAsync(brandId, req.VehicleBrandIds, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task GrantMembershipAsync(
        Guid brandId, Guid companyId, Guid userId, CancellationToken ct = default)
    {
        await RequireOwnedBrandAsync(brandId, companyId, ct);
        _ = await _userRepo.FindAsync(userId, ct) ?? throw new ApiException(ApiErrorCatalog.USER_NOT_FOUND);
        await _repo.GrantMembershipAsync(brandId, userId, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task RevokeMembershipAsync(
        Guid brandId, Guid companyId, Guid userId, CancellationToken ct = default)
    {
        await RequireOwnedBrandAsync(brandId, companyId, ct);
        await _repo.RevokeMembershipAsync(brandId, userId, ct);
        await _uow.SaveChangesAsync(ct);
    }

    private static BrandDto ToDto(Brand b) =>
        new(b.Id, b.CompanyId, b.Name,
            b.Description, b.Phone, b.Email, b.Address, b.LogoUrl,
            b.PrimaryColor, b.IsActive, b.CreatedAt, b.IsAutomotive);
}
