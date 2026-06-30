using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OnTime.Application.DTOs.Vehicles;

public record VehicleBrandDto(
    Guid Id,
    string Name,
    string? LogoUrl,
    int ModelCount = 0
);

public record VehicleVersionDto(
    Guid Id,
    string Name,
    string[] ExternalColors,
    string[] InternalColors
);

public record VehicleModelDto(
    Guid Id,
    Guid BrandId,
    string BrandName,
    string Name,
    string? Version,
    int? Year,
    int? FuelType,
    decimal? BasePrice,
    string? ImageUrl,
    IEnumerable<VehicleVersionDto> Versions,
    bool IsActive = true
);

/// <summary>
/// IsConfigured: true when the model has at least one version with ≥1 exterior colour.
/// Drives the status dot on the Vehicles screen: grey=!IsConfigured, red=!IsActive, green=both.
/// </summary>
public record VehicleModelListDto(
    Guid Id,
    Guid BrandId,
    string BrandName,
    string Name,
    string? Version,
    int? Year,
    int? FuelType,
    bool IsActive = true,
    bool IsConfigured = false
);

public record SetVehicleModelActiveRequest(bool IsActive);

public record CreateVehicleBrandRequest([Required] string Name, string? LogoUrl);
public record CreateVehicleModelRequest(
    [Required] Guid BrandId,
    [Required] string Name,
    string? Version,
    int? Year,
    int? FuelType,
    decimal? BasePrice,
    string? ImageUrl
);
public record UpdateVehicleModelRequest(
    [Required] string Name,
    string? Version,
    int? Year,
    int? FuelType,
    decimal? BasePrice,
    string? ImageUrl
);

public record CreateVehicleVersionRequest(
    [Required] string Name,
    string[]? ExternalColors = null,
    string[]? InternalColors = null
);
public record UpdateVehicleVersionRequest(
    [Required] string Name,
    string[]? ExternalColors = null,
    string[]? InternalColors = null
);

public record VehicleSearchParams(
    string? Search = null,
    Guid? BrandId = null,
    /// <summary>Filter by configured status: true = only configured (≥1 version with a colour),
    /// false = only not-configured, null = both. Frontend defaults this to true.</summary>
    bool? Configured = null,
    int Page = 1,
    int PageSize = 20
);

// Helper for JSON color arrays
public static class ColorArrayHelper
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public static string[] Parse(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json, _opts) ?? []; }
        catch { return []; }
    }

    public static string Serialize(string[]? colors) =>
        JsonSerializer.Serialize(colors ?? []);
}
