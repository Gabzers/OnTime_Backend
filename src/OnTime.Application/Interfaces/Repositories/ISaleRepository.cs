using OnTime.Application.Common;
using OnTime.Application.DTOs.Sales;
using OnTime.Domain.Entities;

namespace OnTime.Application.Interfaces.Repositories;

public interface ISaleRepository
{
    // ── Reads ────────────────────────────────────────────────────────────────

    Task<PagedResult<SaleListDto>> GetPagedAsync(Guid userId, SaleFilterParams filter, CancellationToken ct = default);
    Task<SaleDto?> GetDtoByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Sale?> FindAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Dashboard KPIs + monthly stats + loss reasons + hot deals — all aggregated
    /// in PostgreSQL for a single round-trip.
    /// </summary>
    Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken ct = default);

    // ── Writes ───────────────────────────────────────────────────────────────

    void Add(Sale sale);
}
