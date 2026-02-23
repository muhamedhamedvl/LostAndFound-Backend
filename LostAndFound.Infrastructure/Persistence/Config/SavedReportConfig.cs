using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class SavedReportConfig : IEntityTypeConfiguration<SavedReport>
    {
        public void Configure(EntityTypeBuilder<SavedReport> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Report)
                .WithMany()
                .HasForeignKey(x => x.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ReportId, x.UserId })
                .IsUnique();
        }
    }
}

