using Microsoft.EntityFrameworkCore;
using OnTime.Application.Common;
using OnTime.Application.DTOs.Clients;
using OnTime.Application.DTOs.Sales;
using OnTime.Application.Interfaces.Repositories;
using OnTime.Domain.Entities;
using OnTime.Infrastructure.Persistence;

namespace OnTime.Infrastructure.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _db;

    public SaleRepository(AppDbContext db) => _db = db;

    // ── Paginated list ────────────────────────────────────────────────────
    public async Task<PagedResult<SaleListDto>> GetPagedAsync(
        Guid userId,
        SaleFilterParams filter,
        CancellationToken ct = default)
    {
        var query = _db.Sales
            .AsNoTracking()
            .Include(s => s.Client)
            .Include(s => s.Model)
            .Where(s => s.UserId == userId);

        // Range comparison instead of EXTRACT(year/month FROM sold_at) so a plain
        // btree index on SoldAt can actually be used by the planner.
        if (filter.Year.HasValue)
        {
            var month = filter.Month ?? 1;
            var rangeStart = new DateTimeOffset(filter.Year.Value, month, 1, 0, 0, 0, TimeSpan.Zero);
            var rangeEnd = filter.Month.HasValue
                ? rangeStart.AddMonths(1)
                : rangeStart.AddYears(1);
            query = query.Where(s => s.SoldAt >= rangeStart && s.SoldAt < rangeEnd);
        }
        else if (filter.Month.HasValue)
        {
            query = query.Where(s => s.SoldAt.Month == filter.Month.Value);
        }

        if (filter.Delivered.HasValue)
        {
            query = filter.Delivered.Value
                ? query.Where(s => s.DeliveredAt != null)
                : query.Where(s => s.DeliveredAt == null);
        }

        var total = await query.CountAsync(ct);
        var size  = Math.Clamp(filter.PageSize, 1, 50);

        var items = await query
            .OrderByDescending(s => s.SoldAt)
            .Skip((filter.Page - 1) * size)
            .Take(size)
            .Select(s => new SaleListDto(
                s.Id,
                s.ClientId,
                s.Client.FullName,
                s.Model != null ? $"{s.Model.Brand.Name} {s.Model.Name}" : null,
                s.FreeTextModel,
                s.FinalValue,
                (int)s.PaymentType,
                s.SoldAt,
                s.Plate,
                s.Commission,
                s.EstimatedDeliveryDate,
                s.DeliveredAt))
            .ToListAsync(ct);

        return new PagedResult<SaleListDto>(items, total, filter.Page, size);
    }

    // ── Single sale DTO ───────────────────────────────────────────────────
    public async Task<SaleDto?> GetDtoByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var s = await _db.Sales
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Model)
                .ThenInclude(m => m!.Brand)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

        if (s is null) return null;

        return new SaleDto(
            s.Id, s.ProposalId, s.ClientId, s.Client.FullName, s.Client.Phone,
            s.ModelId,
            s.Model != null ? $"{s.Model.Brand.Name} {s.Model.Name}" : null,
            s.FreeTextModel,
            s.FinalValue, (int)s.PaymentType,
            s.SoldAt,
            s.Plate, s.Chassis, s.Obs,
            s.Commission,
            s.CreatedAt,
            s.EstimatedDeliveryDate,
            s.DeliveredAt);
    }

    public async Task<Sale?> FindAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        await _db.Sales
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

    // ── Dashboard — calls PG functions for all aggregations ───────────────
    public async Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        // 1. KPIs — single PG function call
        var kpiRow = await _db.Database
            .SqlQuery<DashboardKpiRow>($"SELECT * FROM fn_get_dashboard_kpis({userId})")
            .FirstOrDefaultAsync(ct) ?? new DashboardKpiRow(0, 0, 0, 0, 0, 0);

        // 2. Monthly stats — PG function returns 12 rows
        var monthlyRows = await _db.Database
            .SqlQuery<MonthlyStatRow>($"SELECT * FROM fn_get_monthly_stats({userId}, {6})")
            .ToListAsync(ct);

        var monthlyStat = monthlyRows
            .Select(r => new MonthlyStatDto(r.Year, r.Month, r.Proposals, r.Sales, r.Revenue))
            .ToList();

        // 3. Loss reasons — PG function
        var lossRows = await _db.Database
            .SqlQuery<LossReasonRow>($"SELECT * FROM fn_get_loss_reasons({userId})")
            .ToListAsync(ct);

        var lossReasons = lossRows
            .Select(r => new LossReasonStatDto(r.Reason, r.Count))
            .ToList();

        // 4. Hot deals — reuse fn_get_hot_deals (same function as ClientRepository)
        var hotRows = await _db.Database
            .SqlQuery<HotDealRow>($"SELECT * FROM fn_get_hot_deals({userId}, {5})")
            .ToListAsync(ct);

        var hotDeals = hotRows
            .Select<HotDealRow, object>(r => new
            {
                id                = r.Id,
                fullName          = r.FullName,
                phone             = r.Phone,
                stageName         = r.StageName,
                stageColor        = r.StageColor,
                lastInteractionAt = r.LastInteractionAt
            })
            .ToList();

        var conversionRate = kpiRow.TotalProposalsThisMonth > 0
            ? Math.Round((decimal)kpiRow.TotalSalesThisMonth / kpiRow.TotalProposalsThisMonth * 100m, 1)
            : 0m;

        return new DashboardDto(
            (int)kpiRow.TotalClientsActive,
            (int)kpiRow.TotalProposalsThisMonth,
            (int)kpiRow.TotalSalesThisMonth,
            kpiRow.TotalRevenueThisMonth,
            conversionRate,
            kpiRow.TotalCommissionThisMonth,
            monthlyStat,
            lossReasons,
            hotDeals,
            (int)kpiRow.OverdueNotificationsCount);
    }

    // ── Writes ────────────────────────────────────────────────────────────
    public void Add(Sale sale) => _db.Sales.Add(sale);

    // ── Private result types ──────────────────────────────────────────────
    private record DashboardKpiRow(
        long TotalClientsActive,
        long TotalProposalsThisMonth,
        long TotalSalesThisMonth,
        decimal TotalRevenueThisMonth,
        decimal TotalCommissionThisMonth,
        long OverdueNotificationsCount);

    private record MonthlyStatRow(int Year, int Month, int Proposals, int Sales, decimal Revenue);

    private record LossReasonRow(int Reason, int Count);

    private record HotDealRow(
        Guid Id, string FullName, string? Phone, string? Email,
        int LeadSource, int Temperature,
        Guid CurrentStageId, string StageName, string? StageColor,
        DateTimeOffset? LastInteractionAt, DateTimeOffset CreatedAt);
}
