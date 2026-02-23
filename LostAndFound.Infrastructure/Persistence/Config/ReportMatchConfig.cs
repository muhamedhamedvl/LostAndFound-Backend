using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class ReportMatchConfig : IEntityTypeConfiguration<ReportMatch>
    {
        public void Configure(EntityTypeBuilder<ReportMatch> builder)
        {
            builder.HasKey(m => m.Id);

            builder.HasOne(m => m.Report)
                .WithMany()
                .HasForeignKey(m => m.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.MatchedReport)
                .WithMany()
                .HasForeignKey(m => m.MatchedReportId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(m => new { m.ReportId, m.MatchedReportId })
                .IsUnique();
        }
    }
}
