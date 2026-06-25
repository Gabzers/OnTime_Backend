using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnTimeCRM.Domain.Entities;

namespace OnTimeCRM.Infrastructure.Persistence.Configurations;

public class UserVehicleBrandConfiguration : IEntityTypeConfiguration<UserVehicleBrand>
{
    public void Configure(EntityTypeBuilder<UserVehicleBrand> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.VehicleBrandId }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(u => u.SelectedVehicleBrands)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.VehicleBrand)
            .WithMany()
            .HasForeignKey(x => x.VehicleBrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
