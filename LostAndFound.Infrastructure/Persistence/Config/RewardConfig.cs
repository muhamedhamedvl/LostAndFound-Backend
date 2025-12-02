using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class RewardConfig : IEntityTypeConfiguration<Reward>
    {
        public void Configure(EntityTypeBuilder<Reward> builder)
        {
            builder.ToTable("Rewards");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Amount)
                .IsRequired();

            builder.Property(r => r.Currency)
                .IsRequired()
                .HasMaxLength(10);

            builder.HasOne(r => r.Post)
                .WithOne() /
                .HasForeignKey<Reward>(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}