namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Stores device tokens for push notifications (FCM/APNs).
    /// </summary>
    public class DeviceToken : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // "android" or "ios"
    }
}
