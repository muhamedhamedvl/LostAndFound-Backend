// Code written by Mohamed Hamed Mohamed

using System;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents a like on a post.
    /// Users can like posts to show appreciation or interest.
    /// Each user can only like a post once.
    /// </summary>
    public class Like : BaseEntity
    {
        /// <summary>
        /// Foreign key to the post that was liked.
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// Navigation property to the post.
        /// </summary>
        public Post Post { get; set; } = null!;

        /// <summary>
        /// Foreign key to the user who liked the post.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Navigation property to the user who liked the post.
        /// </summary>
        public User User { get; set; } = null!;
    }
}
