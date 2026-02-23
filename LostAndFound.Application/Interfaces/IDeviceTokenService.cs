namespace LostAndFound.Application.Interfaces
{
    public interface IDeviceTokenService
    {
        /// <summary>
        /// Registers or updates a device token for push notifications.
        /// </summary>
        Task RegisterTokenAsync(int userId, string token, string platform);
    }
}
