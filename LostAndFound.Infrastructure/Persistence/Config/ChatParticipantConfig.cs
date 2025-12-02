using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class ChatParticipantConfig : IEntityTypeConfiguration<ChatParticipant>
    {
        public void Configure(EntityTypeBuilder<ChatParticipant> builder)
        {
            builder.ToTable("ChatParticipants");

            builder.HasKey(cp => cp.Id);

            builder.Property(cp => cp.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(cp => cp.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(cp => cp.ChatSession)
                .WithMany(cs => cs.Participants)
                .HasForeignKey(cp => cp.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cp => cp.User)
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

