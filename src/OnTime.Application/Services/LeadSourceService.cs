using OnTime.Application.Common;
using OnTime.Application.DTOs.LeadSources;
using OnTime.Application.Interfaces;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;

namespace OnTime.Application.Services;

public class LeadSourceService : ILeadSourceService
{
    private readonly ILeadSourceRepository _repo;
    private readonly IUnitOfWork _uow;

    public LeadSourceService(ILeadSourceRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow  = uow;
    }

    public async Task<IEnumerable<LeadSourceOptionDto>> GetByCompanyAsync(
        Guid companyId, CancellationToken ct = default)
    {
        var options = await _repo.GetByCompanyAsync(companyId, ct);
        return options.Select(ToDto);
    }

    public async Task<LeadSourceOptionDto> CreateAsync(
        Guid companyId, CreateLeadSourceRequest req, CancellationToken ct = default)
    {
        var code = await _repo.GetNextCodeAsync(companyId, ct);
        var option = new LeadSourceOption { CompanyId = companyId, Code = code, Name = req.Name };
        _repo.Add(option);
        await _uow.SaveChangesAsync(ct);
        return ToDto(option);
    }

    public async Task<LeadSourceOptionDto> UpdateAsync(
        Guid id, Guid companyId, UpdateLeadSourceRequest req, CancellationToken ct = default)
    {
        var option = await RequireOwnedAsync(id, companyId, ct);
        option.Name = req.Name;
        await _uow.SaveChangesAsync(ct);
        return ToDto(option);
    }

    public async Task SetActiveAsync(
        Guid id, Guid companyId, bool isActive, CancellationToken ct = default)
    {
        var option = await RequireOwnedAsync(id, companyId, ct);
        option.IsActive = isActive;
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<LeadSourceOption> RequireOwnedAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var option = await _repo.FindAsync(id, ct)
            ?? throw new ApiException(ApiErrorCatalog.LEAD_SOURCE_NOT_FOUND);
        if (option.CompanyId != companyId)
            throw new ApiException(ApiErrorCatalog.LEAD_SOURCE_WRONG_COMPANY);
        return option;
    }

    private static LeadSourceOptionDto ToDto(LeadSourceOption o) =>
        new(o.Id, o.Code, o.Name, o.IsActive);
}
