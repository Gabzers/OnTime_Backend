using OnTimeCRM.Domain.Common;

namespace OnTimeCRM.Domain.Entities;

/// <summary>
/// The vehicle brands (VehicleBrand catalogue, e.g. XPENG) a user sells.
/// Empty selection for a user = no filter = all brands shown (see USER-BRANDS.md).
/// </summary>
public class UserVehicleBrand : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid VehicleBrandId { get; set; }

    public User User { get; set; } = null!;
    public VehicleBrand VehicleBrand { get; set; } = null!;
}
