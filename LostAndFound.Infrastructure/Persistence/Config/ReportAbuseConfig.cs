using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class ReportAbuseConfig : IEntityTypeConfiguration<ReportAbuse>
    {
        public void Configure(EntityTypeBuilder<ReportAbuse> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reason)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Details)
                .HasMaxLength(2000);

            builder.HasOne(x => x.Report)
                .WithMany()
                .HasForeignKey(x => x.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Reporter)
                .WithMany()
                .HasForeignKey(x => x.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ReportId, x.ReporterId })
                .IsUnique();
        }
    }
}

