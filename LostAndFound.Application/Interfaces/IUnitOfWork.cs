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
        IRepository<ChatSession> ChatSessions { get; }
        IRepository<ChatMessage> ChatMessages { get; }
        IRepository<ChatParticipant> ChatParticipants { get; }
        IRepository<Photo> Photos { get; }
        IRepository<PostImage> PostImages { get; }
        
        // Social features - enabled
        IRepository<Like> Likes { get; }
        IRepository<Comment> Comments { get; }
        IRepository<Share> Shares { get; }
        IRepository<Notification> Notifications { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
