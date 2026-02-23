using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Persistence.Config;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Infrastructure.Persistence
{
    // Identity-backed DbContext using AppUser mapped onto the existing Users table.
    public class AppDbContext : IdentityUserContext<AppUser, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Auth & Users (Identity-backed) - mapped to existing Users table, backward-compatible with legacy User alias
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // Reports
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportImage> ReportImages { get; set; }
        public DbSet<ReportMatch> ReportMatches { get; set; }
        public DbSet<ReportAbuse> ReportAbuses { get; set; }
        public DbSet<SavedReport> SavedReports { get; set; }

        // Categories
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }

        // Chat
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        // Notifications
        public DbSet<Notification> Notifications { get; set; }

        // Push notifications
        public DbSet<DeviceToken> DeviceTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply entity configurations
            modelBuilder.ApplyConfiguration(new UserConfig());
            modelBuilder.ApplyConfiguration(new RoleConfig());
            modelBuilder.ApplyConfiguration(new UserRoleConfig());
            modelBuilder.ApplyConfiguration(new ReportConfig());
            modelBuilder.ApplyConfiguration(new ReportImageConfig());
            modelBuilder.ApplyConfiguration(new ReportMatchConfig());
            modelBuilder.ApplyConfiguration(new ReportAbuseConfig());
            modelBuilder.ApplyConfiguration(new SavedReportConfig());
            modelBuilder.ApplyConfiguration(new CategoryConfig());
            modelBuilder.ApplyConfiguration(new SubCategoryConfig());
            modelBuilder.ApplyConfiguration(new ChatSessionConfig());
            modelBuilder.ApplyConfiguration(new ChatMessageConfig());
            modelBuilder.ApplyConfiguration(new DeviceTokenConfig());

            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Report)
                .WithMany()
                .HasForeignKey(n => n.ReportId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Actor)
                .WithMany()
                .HasForeignKey(n => n.ActorId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
