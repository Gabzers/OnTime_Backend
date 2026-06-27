using OnTime.Application.DTOs.Companies;
using OnTime.Domain.Entities;

namespace OnTime.Application.Interfaces.Repositories;

public interface IAdminRepository
{
    Task<IEnumerable<CompanyAdminDto>> GetCompaniesAsync(CancellationToken ct = default);
    Task<Company?> FindCompanyAsync(Guid id, CancellationToken ct = default);
    void AddCompany(Company company);
}
