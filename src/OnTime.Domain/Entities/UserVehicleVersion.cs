using OnTime.Domain.Common;

namespace OnTime.Domain.Entities;

/// <summary>
/// A specific version/trim of a <see cref="UserVehicleModel"/> — the per-user equivalent
/// of <see cref="VehicleModelVersion"/>. Colour lists are stored as JSON arrays.
/// </summary>
public class UserVehicleVersion : BaseEntity
{
    public Guid ModelId { get; set; }
    public string Name { get; set; } = string.Empty;

    // JSON arrays, e.g. ["Branco","Preto","Cinzento"]
    public string ExternalColors { get; set; } = "[]";
    public string InternalColors { get; set; } = "[]";

    // Navigation
    public UserVehicleModel Model { get; set; } = null!;
}
