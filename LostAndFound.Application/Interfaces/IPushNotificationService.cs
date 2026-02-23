namespace LostAndFound.Application.Interfaces
{
    /// <summary>
    /// Sends push notifications via FCM/APNs.
    /// Implement with FirebaseAdmin SDK for FCM. User provides credentials.
    /// </summary>
    public interface IPushNotificationService
    {
        /// <summary>
        /// Sends a push notification to the user's devices.
        /// </summary>
        Task SendAsync(int userId, string title, string body, Dictionary<string, string>? data = null);
    }
}
