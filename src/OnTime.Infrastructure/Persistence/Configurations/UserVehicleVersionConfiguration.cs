using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnTime.Domain.Entities;

namespace OnTime.Infrastructure.Persistence.Configurations;

public class UserVehicleVersionConfiguration : IEntityTypeConfiguration<UserVehicleVersion>
{
    public void Configure(EntityTypeBuilder<UserVehicleVersion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ExternalColors).HasMaxLength(2000);
        builder.Property(x => x.InternalColors).HasMaxLength(2000);

        builder.HasOne(x => x.Model)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
