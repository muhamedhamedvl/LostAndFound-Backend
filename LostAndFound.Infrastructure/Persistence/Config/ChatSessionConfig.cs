using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Infrastructure.Persistence.Config
{
    public class ChatSessionConfig : IEntityTypeConfiguration<ChatSession>
    {
        public void Configure(EntityTypeBuilder<ChatSession> builder)
        {
            builder.ToTable("ChatSessions");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(c => c.LastMessageTime)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(c => c.User1)
                   .WithMany(u => u.ChatSessions)
                   .HasForeignKey(c => c.User1Id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.User2)
                   .WithMany()
                   .HasForeignKey(c => c.User2Id)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Messages)
                   .WithOne(m => m.ChatSession)
                   .HasForeignKey(m => m.ChatSessionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
