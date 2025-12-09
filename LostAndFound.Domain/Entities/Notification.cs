// Code written by Mohamed Hamed Mohamed

using System;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents a notification sent to a user.
    /// Notifications inform users about likes, comments, messages, and matching posts.
    /// </summary>
    public class Notification : BaseEntity
    {
        /// <summary>
        /// Foreign key to the user receiving the notification.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Navigation property to the user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Type of notification: 'like', 'comment', 'message', 'match', etc.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The notification content/message.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the related post (if applicable).
        /// Nullable for non-post notifications.
        /// </summary>
        public int? PostId { get; set; }

        /// <summary>
        /// Navigation property to the related post.
        /// </summary>
        public Post? Post { get; set; }

        /// <summary>
        /// Foreign key to the user who triggered the notification (actor).
        /// For example, the user who liked a post or sent a message.
        /// </summary>
        public int? ActorId { get; set; }

        /// <summary>
        /// Navigation property to the actor user.
        /// </summary>
        public User? Actor { get; set; }

        /// <summary>
        /// Indicates whether the notification has been read.
        /// </summary>
        public bool IsRead { get; set; } = false;
    }
}
