using LostAndFound.Application.Interfaces;

namespace LostAndFound.Application.Services
{
    /// <summary>
    /// Stub implementation of IPushNotificationService. Does not send push notifications.
    /// Replace with Firebase FCM implementation when credentials are configured.
    /// </summary>
    public class PushNotificationServiceStub : IPushNotificationService
    {
        public Task SendAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
        {
            // No-op until FCM is configured
            return Task.CompletedTask;
        }
    }
}
