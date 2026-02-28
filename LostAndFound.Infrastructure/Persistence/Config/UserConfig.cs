using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class UserConfig : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("Users", t => t.HasCheckConstraint("CHK_Users_Gender", "[Gender] IN ('Male', 'Female', 'Anonymous') OR [Gender] IS NULL"));

            // Global query filter: soft-deleted AND blocked users are excluded from all queries by default.
            // Use .IgnoreQueryFilters() in queries that explicitly need deleted/blocked users (e.g. Admin panels).
            builder.HasQueryFilter(u => !u.IsDeleted && !u.IsBlocked);

            builder.HasKey(u => u.Id);

            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Phone)
                .HasMaxLength(20);

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.IsVerified)
                .HasDefaultValue(false);

            builder.Property(u => u.VerificationCode)
                .HasMaxLength(10)
                .IsRequired(false);

            builder.Property(u => u.VerificationCodeExpiry)
                .IsRequired(false);

            // Profile Information
            builder.Property(u => u.DateOfBirth)
                .IsRequired(false);

            builder.Property(u => u.Gender)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(u => u.ProfilePictureUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(u => u.GoogleId)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.HasIndex(u => u.GoogleId)
                .IsUnique()
                .HasFilter("[GoogleId] IS NOT NULL");

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.UpdatedAt)
                .IsRequired(false);

            builder.HasMany(u => u.Reports)
                .WithOne(r => r.CreatedBy)
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
