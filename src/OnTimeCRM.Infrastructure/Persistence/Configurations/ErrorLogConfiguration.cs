using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnTimeCRM.Domain.Entities;

namespace OnTimeCRM.Infrastructure.Persistence.Configurations;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ErrorCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.Property(x => x.Path).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Method).IsRequired().HasMaxLength(10);
        builder.Property(x => x.TraceId).IsRequired().HasMaxLength(100);

        // Listing is always "most recent first" and often filtered to one user's own errors —
        // no FK to User: a log must survive the user being deleted, and most errors are 4xx
        // client mistakes that don't need a join, just the id for filtering.
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UserId);
    }
}
