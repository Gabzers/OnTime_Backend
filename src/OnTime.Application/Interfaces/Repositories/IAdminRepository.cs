using OnTime.Application.DTOs.Companies;
using OnTime.Application.DTOs.Users;
using OnTime.Domain.Entities;

namespace OnTime.Application.Interfaces.Repositories;

public interface IAdminRepository
{
    Task<IEnumerable<CompanyAdminDto>> GetCompaniesAsync(CancellationToken ct = default);
    Task<Company?> FindCompanyAsync(Guid id, CancellationToken ct = default);
    void AddCompany(Company company);
    Task<IEnumerable<UserListDto>> GetUsersByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<User?> FindUserAsync(Guid userId, CancellationToken ct = default);
    Task<Brand?> FindBrandAsync(Guid brandId, CancellationToken ct = default);
    Task GrantMembershipAsync(Guid userId, Guid brandId, CancellationToken ct = default);
    Task RevokeMembershipAsync(Guid userId, Guid brandId, CancellationToken ct = default);
}
