using LostAndFound.Application.DTOs.Notification;
using LostAndFound.Domain.Entities;

namespace LostAndFound.Application.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateNotificationAsync(int userId, string title, string message, int? relatedPostId = null);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId);
        Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(int userId);
        Task<NotificationDto?> GetNotificationByIdAsync(int id, int userId);
        Task<NotificationDto> MarkAsReadAsync(int id, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteNotificationAsync(int id, int userId);
        Task<NotificationDto?> NotifyPostOwnerAboutCommentAsync(int postId, int commenterId);
        Task<NotificationDto?> NotifyPostOwnerAboutLikeAsync(int postId, int likerId);
        Task<NotificationDto?> NotifyMatchingPostAsync(Post ownerPost, Post newPost);
    }
}

