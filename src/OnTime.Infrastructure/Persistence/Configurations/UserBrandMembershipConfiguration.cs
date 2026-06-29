using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnTime.Domain.Entities;

namespace OnTime.Infrastructure.Persistence.Configurations;

public class UserBrandMembershipConfiguration : IEntityTypeConfiguration<UserBrandMembership>
{
    public void Configure(EntityTypeBuilder<UserBrandMembership> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.BrandId }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(u => u.BrandMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Brand)
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
