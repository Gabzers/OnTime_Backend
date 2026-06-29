using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnTime.Domain.Entities;

namespace OnTime.Infrastructure.Persistence.Configurations;

public class LeadSourceOptionConfiguration : IEntityTypeConfiguration<LeadSourceOption>
{
    public void Configure(EntityTypeBuilder<LeadSourceOption> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}
