using OnTime.Domain.Common;
using OnTime.Domain.Enums;

namespace OnTime.Domain.Entities;

/// <summary>
/// A user's own copy of a vehicle model. Cloned from the global <see cref="VehicleModel"/>
/// catalog when the user selects a <see cref="VehicleBrand"/> (see UserVehicleBrand), or
/// created directly by the user. IsActive=false means the owning brand was unselected —
/// hidden everywhere, not deleted, so re-selecting the brand restores it.
/// </summary>
public class UserVehicleModel : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid VehicleBrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public int? Year { get; set; }
    public FuelType? FuelType { get; set; }
    public decimal? BasePrice { get; set; }
    public string? ImageUrl { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public VehicleBrand VehicleBrand { get; set; } = null!;
    public ICollection<UserVehicleVersion> Versions { get; set; } = new List<UserVehicleVersion>();
    public ICollection<ProposalVehicle> ProposalVehicles { get; set; } = new List<ProposalVehicle>();
}
