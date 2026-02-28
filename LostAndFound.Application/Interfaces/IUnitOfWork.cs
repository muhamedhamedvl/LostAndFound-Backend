using LostAndFound.Domain.Entities;

namespace LostAndFound.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<AppUser> Users { get; }
        IRepository<Role> Roles { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<Report> Reports { get; }
        IRepository<ReportImage> ReportImages { get; }
        IRepository<ReportMatch> ReportMatches { get; }
        IRepository<ReportAbuse> ReportAbuses { get; }
        IRepository<SavedReport> SavedReports { get; }
        IRepository<Category> Categories { get; }
        IRepository<SubCategory> SubCategories { get; }
        IRepository<ChatSession> ChatSessions { get; }
        IRepository<ChatMessage> ChatMessages { get; }
        IRepository<Notification> Notifications { get; }
        IRepository<DeviceToken> DeviceTokens { get; }
        IRepository<RefreshToken> RefreshTokens { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
