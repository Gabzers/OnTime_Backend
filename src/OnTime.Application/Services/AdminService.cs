using OnTime.Application.Common;
using OnTime.Application.DTOs.Companies;
using OnTime.Application.DTOs.Users;
using OnTime.Application.Interfaces;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;
using OnTime.Domain.Enums;

namespace OnTime.Application.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly IUnitOfWork      _uow;

    public AdminService(IAdminRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public Task<IEnumerable<CompanyAdminDto>> GetCompaniesAsync(
        CancellationToken ct = default) =>
        _repo.GetCompaniesAsync(ct);

    public async Task<CompanyAdminDto> CreateCompanyAsync(
        CreateCompanyAdminRequest request, CancellationToken ct = default)
    {
        var company = new Company
        {
            Name    = request.Name,
            Phone   = request.Phone,
            Email   = request.Email,
            Address = request.Address,
            IsActive = true
        };

        _repo.AddCompany(company);
        await _uow.SaveChangesAsync(ct);

        return new CompanyAdminDto(company.Id, company.Name, company.Phone,
            company.Email, company.Address, company.IsActive, 0, 0, company.CreatedAt);
    }

    public async Task<CompanyAdminDto> UpdateCompanyAsync(
        Guid id, UpdateCompanyAdminRequest request, CancellationToken ct = default)
    {
        var company = await _repo.FindCompanyAsync(id, ct)
            ?? throw new ApiException(ApiErrorCatalog.COMPANY_NOT_FOUND);

        company.Name    = request.Name;
        company.Phone   = request.Phone;
        company.Email   = request.Email;
        company.Address = request.Address;

        await _uow.SaveChangesAsync(ct);

        return new CompanyAdminDto(company.Id, company.Name, company.Phone,
            company.Email, company.Address, company.IsActive, 0, 0, company.CreatedAt);
    }

    public async Task SetCompanyActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var company = await _repo.FindCompanyAsync(id, ct)
            ?? throw new ApiException(ApiErrorCatalog.COMPANY_NOT_FOUND);

        company.IsActive = isActive;
        await _uow.SaveChangesAsync(ct);
    }

    public Task<IEnumerable<UserListDto>> GetUsersByCompanyAsync(
        Guid companyId, CancellationToken ct = default) =>
        _repo.GetUsersByCompanyAsync(companyId, ct);

    public async Task<UserListDto> UpdateUserRoleAsync(
        Guid userId, int role, Guid actingUserId, CancellationToken ct = default)
    {
        if (userId == actingUserId)
            throw new ApiException(ApiErrorCatalog.CANNOT_CHANGE_OWN_ROLE);

        if (role < 0 || role > 2)
            throw new ApiException(ApiErrorCatalog.INVALID_ROLE);

        var user = await _repo.FindUserAsync(userId, ct)
            ?? throw new ApiException(ApiErrorCatalog.USER_NOT_FOUND);

        user.Role = (UserRole)role;
        await _uow.SaveChangesAsync(ct);

        return new UserListDto(
            user.Id, user.FullName, user.Email, user.Phone,
            (int)user.Role, (int)user.AccountStatus, user.CreatedAt);
    }

    public async Task GrantMembershipAsync(Guid userId, Guid brandId, CancellationToken ct = default)
    {
        _ = await _repo.FindUserAsync(userId, ct) ?? throw new ApiException(ApiErrorCatalog.USER_NOT_FOUND);
        _ = await _repo.FindBrandAsync(brandId, ct) ?? throw new ApiException(ApiErrorCatalog.BRAND_NOT_FOUND);
        await _repo.GrantMembershipAsync(userId, brandId, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task RevokeMembershipAsync(Guid userId, Guid brandId, CancellationToken ct = default)
    {
        await _repo.RevokeMembershipAsync(userId, brandId, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
