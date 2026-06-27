namespace OnTime.Application.DTOs.Sales;

public record SaleDto(
    Guid Id,
    Guid ProposalId,
    Guid ClientId,
    string ClientName,
    string? ClientPhone,
    Guid? ModelId,
    string? ModelName,
    string? FreeTextModel,
    decimal FinalValue,
    int PaymentType,
    DateTimeOffset SoldAt,
    string? Plate,
    string? Chassis,
    string? Obs,
    /// <summary>Commission is private — only returned to the owner; never included in friend public profiles.</summary>
    decimal? Commission,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EstimatedDeliveryDate = null,
    DateTimeOffset? DeliveredAt = null
);

public record SaleListDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string? ModelName,
    string? FreeTextModel,
    decimal FinalValue,
    int PaymentType,
    DateTimeOffset SoldAt,
    string? Plate = null,
    decimal? Commission = null,
    DateTimeOffset? EstimatedDeliveryDate = null,
    DateTimeOffset? DeliveredAt = null
);

public record UpdateSaleRequest(
    decimal? FinalValue = null,
    int? PaymentType = null,
    DateTimeOffset? SoldAt = null,
    Guid? ModelId = null,
    string? FreeTextModel = null,
    string? Plate = null,
    string? Chassis = null,
    string? Obs = null,
    decimal? Commission = null,
    DateTimeOffset? EstimatedDeliveryDate = null,
    DateTimeOffset? DeliveredAt = null
);

public record ClientSaleHistoryDto(
    Guid Id,
    string? ModelName,
    string? FreeTextModel,
    decimal FinalValue,
    int PaymentType,
    DateTimeOffset SoldAt
);

public record SaleFilterParams(
    int? Year = null,
    int? Month = null,
    /// <summary>Filter by delivery status: null = all, true = delivered only, false = pending only.</summary>
    bool? Delivered = null,
    int Page = 1,
    int PageSize = 20
);

public record DashboardDto(
    int ActiveClients,
    int ProposalsThisMonth,
    int SalesThisMonth,
    decimal RevenueThisMonth,
    decimal ConversionRate,
    decimal CommissionThisMonth,
    IEnumerable<MonthlyStatDto> MonthlySales,
    IEnumerable<LossReasonStatDto> LossReasons,
    IEnumerable<object> HotDeals,
    int OverdueNotificationsCount
);

public record MonthlyStatDto(int Year, int Month, int Proposals, int Sales, decimal Revenue);
public record LossReasonStatDto(int LossReason, int Count);
