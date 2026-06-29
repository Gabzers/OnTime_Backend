using OnTime.Domain.Common;

namespace OnTime.Domain.Entities;

/// <summary>
/// The vehicle brands (VehicleBrand catalogue, e.g. XPENG) a Filial (<see cref="Brand"/>) sells —
/// configured by Manager/Admin, not per-user. Replaces the old per-user <c>UserVehicleBrand</c>
/// (see 04-DECISIONS/2026-06-27-filial-vehicle-brands.md). Every user whose currently-active Filial
/// has a row here is allowed to clone/sell that VehicleBrand in their own personal catalog
/// (UserVehicleModel/UserVehicleVersion) — see USER-BRANDS.md.
/// </summary>
public class BrandVehicleBrand : BaseEntity
{
    public Guid BrandId { get; set; }
    public Guid VehicleBrandId { get; set; }

    public Brand Brand { get; set; } = null!;
    public VehicleBrand VehicleBrand { get; set; } = null!;
}
