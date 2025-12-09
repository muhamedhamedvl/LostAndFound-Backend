// Code written by Mohamed Hamed Mohamed

using System;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents a share of a post.
    /// Users can share posts to spread awareness about lost or found items.
    /// </summary>
    public class Share : BaseEntity
    {
        /// <summary>
        /// Foreign key to the post being shared.
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// Navigation property to the post.
        /// </summary>
        public Post Post { get; set; } = null!;

        /// <summary>
        ///Foreign key to the user who shared the post.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Navigation property to the user who shared.
        /// </summary>
        public User User { get; set; } = null!;
    }
}
