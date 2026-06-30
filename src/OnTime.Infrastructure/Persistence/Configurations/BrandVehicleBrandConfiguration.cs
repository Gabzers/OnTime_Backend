using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnTime.Domain.Entities;

namespace OnTime.Infrastructure.Persistence.Configurations;

public class BrandVehicleBrandConfiguration : IEntityTypeConfiguration<BrandVehicleBrand>
{
    public void Configure(EntityTypeBuilder<BrandVehicleBrand> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.BrandId, x.VehicleBrandId }).IsUnique();

        builder.HasOne(x => x.Brand)
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.VehicleBrand)
            .WithMany()
            .HasForeignKey(x => x.VehicleBrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
