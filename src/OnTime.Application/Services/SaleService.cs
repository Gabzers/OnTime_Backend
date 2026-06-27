using OnTime.Application.Common;
using OnTime.Application.DTOs.Sales;
using OnTime.Application.Interfaces;
using OnTime.Application.Interfaces.Repositories;

namespace OnTime.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _repo;
    private readonly IUnitOfWork _uow;

    public SaleService(ISaleRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public Task<PagedResult<SaleListDto>> GetPagedAsync(
        Guid userId, SaleFilterParams filter, CancellationToken ct = default) =>
        _repo.GetPagedAsync(userId, filter, ct);

    public async Task<SaleDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken ct = default)
    {
        // Repository filters by both id and userId — not found means either missing or wrong user
        var dto = await _repo.GetDtoByIdAsync(id, userId, ct)
            ?? throw new ApiException(ApiErrorCatalog.SALE_NOT_FOUND);
        return dto;
    }

    public async Task<SaleDto> UpdateAsync(
        Guid id, Guid userId, UpdateSaleRequest request, CancellationToken ct = default)
    {
        var sale = await _repo.FindAsync(id, userId, ct)
            ?? throw new ApiException(ApiErrorCatalog.SALE_NOT_FOUND);

        if (request.FinalValue.HasValue)
            sale.FinalValue = request.FinalValue.Value;

        if (request.PaymentType.HasValue)
            sale.PaymentType = (Domain.Enums.PaymentType)request.PaymentType.Value;

        if (request.SoldAt.HasValue)
            sale.SoldAt = request.SoldAt.Value;

        if (request.ModelId.HasValue || request.FreeTextModel is not null)
        {
            sale.ModelId = request.ModelId;
            sale.FreeTextModel = request.FreeTextModel;
        }

        if (request.Plate is not null)
            sale.Plate = request.Plate;

        if (request.Chassis is not null)
            sale.Chassis = request.Chassis;

        if (request.Obs is not null)
            sale.Obs = request.Obs;

        if (request.Commission.HasValue)
            sale.Commission = request.Commission.Value;

        if (request.EstimatedDeliveryDate.HasValue)
            sale.EstimatedDeliveryDate = request.EstimatedDeliveryDate.Value;

        if (request.DeliveredAt.HasValue)
            sale.DeliveredAt = request.DeliveredAt.Value;

        await _uow.SaveChangesAsync(ct);

        return await _repo.GetDtoByIdAsync(id, userId, ct)
            ?? throw new ApiException(ApiErrorCatalog.SALE_NOT_FOUND);
    }

    public Task<DashboardDto> GetDashboardAsync(
        Guid userId, CancellationToken ct = default) =>
        _repo.GetDashboardAsync(userId, ct);
}
