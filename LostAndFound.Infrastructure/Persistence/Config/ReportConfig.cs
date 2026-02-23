using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class ReportConfig : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(r => r.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(r => r.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(r => r.LifecycleStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(r => r.DateReported)
                .HasColumnType("datetime2");

            builder.Property(r => r.LocationName)
                .HasMaxLength(300);

            builder.HasOne(r => r.CreatedBy)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.SubCategory)
                .WithMany(sc => sc.Reports)
                .HasForeignKey(r => r.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.Images)
                .WithOne(i => i.Report)
                .HasForeignKey(i => i.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common filters
            builder.HasIndex(r => r.LifecycleStatus);
            builder.HasIndex(r => r.DateReported);
            builder.HasIndex(r => r.SubCategoryId);
            builder.HasIndex(r => new { r.Latitude, r.Longitude });
        }
    }
}
