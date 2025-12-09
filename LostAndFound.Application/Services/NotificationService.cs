using AutoMapper;
using LostAndFound.Application.DTOs.Notification;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<NotificationDto> CreateNotificationAsync(int userId, string title, string message, int? relatedPostId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = title,  // Using Type field for notification type
                Content = message,
                PostId = relatedPostId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId)
        {
            var notifications = await _unitOfWork.Notifications
                .GetQueryable()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(int userId)
        {
            var notifications = await _unitOfWork.Notifications
                .GetQueryable()
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<NotificationDto?> GetNotificationByIdAsync(int id, int userId)
        {
            var notification = await _unitOfWork.Notifications
                .GetQueryable()
                .Where(n => n.Id == id && n.UserId == userId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return notification != null ? _mapper.Map<NotificationDto>(notification) : null;
        }

        public async Task<NotificationDto> MarkAsReadAsync(int id, int userId)
        {
            var notification = await _unitOfWork.Notifications
                .GetQueryable()
                .Where(n => n.Id == id && n.UserId == userId)
                .FirstOrDefaultAsync();

            if (notification == null)
            {
                throw new KeyNotFoundException("Notification not found");
            }

            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Notifications.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _unitOfWork.Notifications
                .GetQueryable()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Notifications.UpdateAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int id, int userId)
        {
            var notification = await _unitOfWork.Notifications
                .GetQueryable()
                .Where(n => n.Id == id && n.UserId == userId)
                .FirstOrDefaultAsync();

            if (notification == null)
            {
                throw new KeyNotFoundException("Notification not found");
            }

            await _unitOfWork.Notifications.DeleteAsync(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<NotificationDto?> NotifyPostOwnerAboutCommentAsync(int postId, int commenterId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.CreatorId == commenterId)
            {
                return null; // Don't notify if post not found or commenter is the owner
            }

            var commenter = await _unitOfWork.Users.GetByIdAsync(commenterId);
            if (commenter == null)
            {
                return null;
            }

            var notification = new Notification
            {
                UserId = post.CreatorId,
                Type = "comment",
                Content = $"{commenter.FullName} commented on your post",
                PostId = postId,
                ActorId = commenterId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<NotificationDto?> NotifyPostOwnerAboutLikeAsync(int postId, int likerId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.CreatorId == likerId)
            {
                return null; // Don't notify if post not found or liker is the owner
            }

            var liker = await _unitOfWork.Users.GetByIdAsync(likerId);
            if (liker == null)
            {
                return null;
            }

            var notification = new Notification
            {
                UserId = post.CreatorId,
                Type = "like",
                Content = $"{liker.FullName} liked your post",
                PostId = postId,
                ActorId = likerId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<NotificationDto?> NotifyMatchingPostAsync(Post ownerPost, Post newPost)
        {
            if (ownerPost == null || newPost == null || ownerPost.CreatorId == newPost.CreatorId)
            {
                return null; // Don't notify if same user
            }

            // Check if posts are in the same category
            if (ownerPost.SubCategoryId != newPost.SubCategoryId)
            {
                return null; // Different categories
            }

            var notification = new Notification
            {
                UserId = ownerPost.CreatorId,
                Type = "match",
                Content = $"A new post might match your lost/found item",
                PostId = newPost.Id,
                ActorId = newPost.CreatorId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<NotificationDto>(notification);
        }
    }
}
