using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class DeviceTokenConfig : IEntityTypeConfiguration<DeviceToken>
    {
        public void Configure(EntityTypeBuilder<DeviceToken> builder)
        {
            builder.ToTable("DeviceTokens");

            builder.HasKey(dt => dt.Id);

            builder.Property(dt => dt.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(dt => dt.UpdatedAt)
                .IsRequired(false);

            builder.Property(dt => dt.Token)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(dt => dt.Platform)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasOne(dt => dt.User)
                .WithMany()
                .HasForeignKey(dt => dt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(dt => new { dt.UserId, dt.Token })
                .IsUnique();
        }
    }
}
