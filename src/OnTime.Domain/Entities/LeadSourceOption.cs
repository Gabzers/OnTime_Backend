using OnTime.Domain.Common;

namespace OnTime.Domain.Entities;

/// <summary>
/// Company-configurable lead source (e.g. "Stand", "Instagram"). Replaces the old fixed
/// LeadSource enum as the canonical list — see ROADMAP.md. <see cref="Code"/> is the value
/// stored on <see cref="Client.LeadSource"/>, unique per company (not globally).
/// </summary>
public class LeadSourceOption : BaseEntity
{
    public Guid CompanyId { get; set; }
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;

    public Company Company { get; set; } = null!;
}
