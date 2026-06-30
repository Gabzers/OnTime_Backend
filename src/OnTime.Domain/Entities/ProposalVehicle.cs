using OnTime.Domain.Common;

namespace OnTime.Domain.Entities;

public class ProposalVehicle : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid? ModelId { get; set; }         // null if free-text — FK to UserVehicleModel (owner's own catalog)
    public string? FreeTextModel { get; set; }
    public bool IsPreferred { get; set; } = false;
    /// <summary>Price for this specific vehicle in this proposal.</summary>
    public decimal? Price { get; set; }
    /// <summary>Discount applied to this specific vehicle.</summary>
    public decimal? Discount { get; set; }
    public string? Obs { get; set; }
    public Guid? VersionId { get; set; }
    public string? ExternalColor { get; set; }
    public string? InternalColor { get; set; }
    /// <summary>Optional — a used/pre-registered vehicle may already have a plate; a genuinely new one won't.</summary>
    public string? Plate { get; set; }

    // Navigation
    public Proposal Proposal { get; set; } = null!;
    public UserVehicleModel? Model { get; set; }
    public UserVehicleVersion? Version { get; set; }
}
