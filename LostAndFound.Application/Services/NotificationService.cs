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
        private readonly IRealtimeNotificationSender _realtimeNotificationSender;
        private readonly IPushNotificationService _pushNotificationService;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, IRealtimeNotificationSender realtimeNotificationSender, IPushNotificationService pushNotificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _realtimeNotificationSender = realtimeNotificationSender;
            _pushNotificationService = pushNotificationService;
        }

        public async Task<NotificationDto> CreateNotificationAsync(int userId, string type, string message, int? reportId = null, string? title = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title ?? type,
                Content = message,
                ReportId = reportId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<(IEnumerable<NotificationDto> Notifications, int TotalCount)> GetUserNotificationsAsync(int userId, string? typeFilter = null, string? category = null, int page = 1, int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _unitOfWork.Notifications
                .GetQueryable()
                .Where(n => n.UserId == userId);

            // Filter by read/unread status
            if (string.Equals(typeFilter, "unread", StringComparison.OrdinalIgnoreCase))
                query = query.Where(n => !n.IsRead);
            else if (string.Equals(typeFilter, "read", StringComparison.OrdinalIgnoreCase))
                query = query.Where(n => n.IsRead);

            // Filter by notification category (all/general/matches)
            if (!string.IsNullOrEmpty(category) && !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(category, "matches", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(n => n.Type == "match");
                }
                else if (string.Equals(category, "general", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(n => n.Type != "match");
                }
            }

            var totalCount = await query.CountAsync();

            var notifications = await query
                .Include(n => n.Actor)
                .Include(n => n.Report)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (_mapper.Map<IEnumerable<NotificationDto>>(notifications), totalCount);
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

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _unitOfWork.Notifications
                .GetQueryable()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<NotificationDto?> GetNotificationByIdAsync(int id, int userId)
        {
            var notification = await _unitOfWork.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            return notification != null ? _mapper.Map<NotificationDto>(notification) : null;
        }

        public async Task<NotificationDto> MarkAsReadAsync(int id, int userId)
        {
            var notification = await _unitOfWork.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                throw new KeyNotFoundException("Notification not found");

            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Notifications.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            var notifications = await _unitOfWork.Notifications
                .FindAsync(n => n.UserId == userId && !n.IsRead);

            var count = 0;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = DateTime.UtcNow;
                count++;
            }

            await _unitOfWork.SaveChangesAsync();
            return count;
        }

        public async Task DeleteNotificationAsync(int id, int userId)
        {
            var notification = await _unitOfWork.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                throw new KeyNotFoundException("Notification not found");

            await _unitOfWork.Notifications.DeleteAsync(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<NotificationDto?> NotifyReportMatchAsync(Report report, Report matchedReport, double score)
        {
            if (report.CreatedById == matchedReport.CreatedById)
                return null;

            // Prevent duplicate
            var exists = await _unitOfWork.Notifications.ExistsAsync(n =>
                n.UserId == report.CreatedById && n.Type == "match" && n.ReportId == matchedReport.Id);
            if (exists) return null;

            var notification = new Notification
            {
                UserId = report.CreatedById,
                Type = "match",
                Title = "Item Match Found!",
                Content = $"A matching report was found with {score:F0}% similarity",
                ReportId = matchedReport.Id,
                ActorId = matchedReport.CreatedById,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<NotificationDto>(notification);
            try
            {
                await _realtimeNotificationSender.SendAsync(dto);
            }
            catch { /* Don't fail if SignalR send fails */ }
            try
            {
                await _pushNotificationService.SendAsync(dto.UserId, dto.Title, dto.Message, new Dictionary<string, string> { ["reportId"] = matchedReport.Id.ToString() });
            }
            catch { /* Don't fail if push fails */ }
            return dto;
        }

        public async Task<NotificationDto?> NotifyReportStatusChangeAsync(int reportId, string newStatus)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null) return null;

            var notification = new Notification
            {
                UserId = report.CreatedById,
                Type = "status_update",
                Title = "Status Updated",
                Content = $"Your report status has been updated to {newStatus}",
                ReportId = reportId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<NotificationDto>(notification);
            try
            {
                await _realtimeNotificationSender.SendAsync(dto);
            }
            catch { /* Don't fail if SignalR send fails */ }
            try
            {
                await _pushNotificationService.SendAsync(dto.UserId, dto.Title, dto.Message, new Dictionary<string, string> { ["reportId"] = reportId.ToString() });
            }
            catch { /* Don't fail if push fails */ }
            return dto;
        }

        public async Task<NotificationDto?> NotifyInterestedInReportAsync(int reportId, int interestedUserId)
        {
            var report = await _unitOfWork.Reports.GetByIdAsync(reportId);
            if (report == null) return null;

            // Don't notify yourself
            if (report.CreatedById == interestedUserId) return null;

            var interestedUser = await _unitOfWork.Users.GetByIdAsync(interestedUserId);
            if (interestedUser == null) return null;

            var notification = new Notification
            {
                UserId = report.CreatedById,
                Type = "interested",
                Title = "Interested in Report!",
                Content = $"{interestedUser.FullName} is interested in your report",
                ReportId = reportId,
                ActorId = interestedUserId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<NotificationDto>(notification);
            try
            {
                await _realtimeNotificationSender.SendAsync(dto);
            }
            catch { /* Don't fail if SignalR send fails */ }
            try
            {
                await _pushNotificationService.SendAsync(dto.UserId, dto.Title, dto.Message, new Dictionary<string, string> { ["reportId"] = reportId.ToString() });
            }
            catch { /* Don't fail if push fails */ }
            return dto;
        }

        public async Task<NotificationDto?> NotifyNewMessageAsync(int recipientUserId, int senderUserId, string senderName, int chatSessionId)
        {
            // Don't notify yourself
            if (recipientUserId == senderUserId) return null;

            var notification = new Notification
            {
                UserId = recipientUserId,
                Type = "new_message",
                Title = "New Message",
                Content = $"You have a new message from {senderName}",
                ActorId = senderUserId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<NotificationDto>(notification);
            try
            {
                await _realtimeNotificationSender.SendAsync(dto);
            }
            catch { /* Don't fail if SignalR send fails */ }
            try
            {
                await _pushNotificationService.SendAsync(dto.UserId, dto.Title, dto.Message, new Dictionary<string, string> { ["chatSessionId"] = chatSessionId.ToString() });
            }
            catch { /* Don't fail if push fails */ }
            return dto;
        }

        public async Task<NotificationDto?> NotifyLocationAlertAsync(int userId, int reportId, string reportTitle, double distanceKm)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = "location_alert",
                Title = "Location Alert!",
                Content = $"A report '{reportTitle}' was found {distanceKm:F1}km from your location",
                ReportId = reportId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<NotificationDto>(notification);
            try
            {
                await _realtimeNotificationSender.SendAsync(dto);
            }
            catch { /* Don't fail if SignalR send fails */ }
            try
            {
                await _pushNotificationService.SendAsync(dto.UserId, dto.Title, dto.Message, new Dictionary<string, string> { ["reportId"] = reportId.ToString() });
            }
            catch { /* Don't fail if push fails */ }
            return dto;
        }
    }
}
