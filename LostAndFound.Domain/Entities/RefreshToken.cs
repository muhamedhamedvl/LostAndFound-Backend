namespace LostAndFound.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }

        /// <summary>
        /// The opaque refresh-token string sent to the client.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// UTC expiry date of this token.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// UTC date this token was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// UTC date this token was revoked (null if still active).
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Optional device/client identifier (e.g. "iPhone-14", "Chrome-Win").
        /// </summary>
        public string? DeviceInfo { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsExpired;

        // Foreign key
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
    }
}
