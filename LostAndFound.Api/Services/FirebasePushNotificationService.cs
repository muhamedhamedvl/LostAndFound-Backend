using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using LostAndFound.Application.Interfaces;
using LostAndFound.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Services
{
    /// <summary>
    /// Sends push notifications via Firebase Cloud Messaging (FCM).
    /// Works for Web, Android, and iOS clients that register FCM device tokens.
    /// </summary>
    public class FirebasePushNotificationService : IPushNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly FirebaseOptions _options;

        public FirebasePushNotificationService(IUnitOfWork unitOfWork, IOptions<FirebaseOptions> options)
        {
            _unitOfWork = unitOfWork;
            _options = options.Value;
        }

        public async Task SendAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
        {
            var tokens = await _unitOfWork.DeviceTokens
                .GetQueryable()
                .Where(dt => dt.UserId == userId && !string.IsNullOrWhiteSpace(dt.Token))
                .Select(dt => dt.Token)
                .Distinct()
                .ToListAsync();

            if (tokens.Count == 0)
                return;

            var dataPayload = data ?? new Dictionary<string, string>();

            foreach (var token in tokens)
            {
                try
                {
                    var message = new Message
                    {
                        Token = token,
                        Notification = new Notification
                        {
                            Title = title,
                            Body = body
                        },
                        Data = dataPayload,
                        Android = new AndroidConfig
                        {
                            Priority = Priority.High,
                            Notification = new AndroidNotification
                            {
                                Title = title,
                                Body = body,
                                ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                            }
                        },
                        Apns = new ApnsConfig
                        {
                            Aps = new Aps
                            {
                                Alert = new ApsAlert { Title = title, Body = body },
                                Sound = "default",
                                ContentAvailable = true
                            }
                        },
                        Webpush = new WebpushConfig
                        {
                            Notification = new WebpushNotification
                            {
                                Title = title,
                                Body = body
                            }
                        }
                    };

                    await FirebaseMessaging.DefaultInstance.SendAsync(message);
                }
                catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument || ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
                {
                    // Invalid or expired token - could remove from DB (optional)
                    // Continue sending to other tokens
                }
            }
        }
    }
}
