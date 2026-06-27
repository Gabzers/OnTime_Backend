using OnTime.Domain.Common;

namespace OnTime.Domain.Entities;

public class TranslationEntry : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Locale { get; set; } = "pt-PT";
    public string Value { get; set; } = string.Empty;
}
