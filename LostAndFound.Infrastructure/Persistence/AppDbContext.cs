using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Persistence.Config;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Infrastructure.Persistence
{
    /// <summary>
    /// Entity Framework Core database context for the Lost and Found application.
    /// Manages database connections and entity configurations.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the AppDbContext.
        /// </summary>
        /// <param name="options">DbContext options including connection string</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<PostImage> PostImages { get; set; }

        /// <summary>
        /// Configures the database model and applies entity configurations.
        /// Sets up relationships, constraints, and ignores entities disabled for MVP.
        /// </summary>
        /// <param name="modelBuilder">Model builder for configuring entities</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new UserConfig());
            modelBuilder.ApplyConfiguration(new RoleConfig());
            modelBuilder.ApplyConfiguration(new UserRoleConfig());
            modelBuilder.ApplyConfiguration(new PostConfig());
            modelBuilder.ApplyConfiguration(new PostImageConfig());
            modelBuilder.ApplyConfiguration(new CategoryConfig());
            modelBuilder.ApplyConfiguration(new SubCategoryConfig());
            modelBuilder.ApplyConfiguration(new ChatSessionConfig());
            modelBuilder.ApplyConfiguration(new ChatMessageConfig());
            modelBuilder.ApplyConfiguration(new ChatParticipantConfig());
            modelBuilder.ApplyConfiguration(new PhotoConfig());
        }
    }
}
