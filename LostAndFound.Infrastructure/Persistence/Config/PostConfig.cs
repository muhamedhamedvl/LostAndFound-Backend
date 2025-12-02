using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class PostConfig : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.ToTable("Posts", t => t.HasCheckConstraint("CHK_Posts_Status", "[Status] IN ('Active', 'Resolved', 'Closed')"));

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Content)
                .IsRequired()
                .HasMaxLength(2000);

            builder.HasOne(p => p.SubCategory)
                .WithMany(sc => sc.Posts)
                .HasForeignKey(p => p.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            builder.Property(p => p.ResolvedAt)
                .IsRequired(false);

            builder.HasOne(p => p.ResolvedByUser)
                .WithMany()
                .HasForeignKey(p => p.ResolvedByUserId)
                .OnDelete(DeleteBehavior.SetNull); 

            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(p => p.Creator)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Photos)
                .WithOne(ph => ph.Post)
                .HasForeignKey(ph => ph.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(p => p.Latitude)
                .HasColumnType("float")
                .IsRequired(false);

            builder.Property(p => p.Longitude)
                .HasColumnType("float")
                .IsRequired(false);

            builder.Property(p => p.Address)
                .HasMaxLength(250)
                .IsRequired(false);

            builder.Property(p => p.RewardAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);

            builder.Property(p => p.PlatformFeeAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);

            builder.HasMany(p => p.PostImages)
                .WithOne(pi => pi.Post)
                .HasForeignKey(pi => pi.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

