using LostAndFound.Domain.Entities;

namespace LostAndFound.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Role> Roles { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<Post> Posts { get; }
        IRepository<Category> Categories { get; }
        IRepository<SubCategory> SubCategories { get; }
        // TEMPORARILY DISABLED FOR MVP - Commented out to prevent EF Core tracking
        //IRepository<Comment> Comments { get; }
        //IRepository<Location> Locations { get; }
        IRepository<ChatSession> ChatSessions { get; }
        IRepository<ChatMessage> ChatMessages { get; }
        IRepository<ChatParticipant> ChatParticipants { get; }
        // TEMPORARILY DISABLED FOR MVP - Commented out to prevent EF Core tracking
        //IRepository<Notification> Notifications { get; }
        //IRepository<Reward> Rewards { get; }
        IRepository<Photo> Photos { get; }
        IRepository<PostImage> PostImages { get; }
        //IRepository<Like> Likes { get; }
        //IRepository<Share> Shares { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
