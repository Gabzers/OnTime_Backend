using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnTime.Domain.Entities;

namespace OnTime.Infrastructure.Persistence.Configurations;

public class UserVehicleModelConfiguration : IEntityTypeConfiguration<UserVehicleModel>
{
    public void Configure(EntityTypeBuilder<UserVehicleModel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Version).HasMaxLength(100);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.BasePrice).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.VehicleBrand)
            .WithMany()
            .HasForeignKey(x => x.VehicleBrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
