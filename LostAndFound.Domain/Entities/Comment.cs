// Code written by Mohamed Hamed Mohamed

using System;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents a comment on a post.
    /// Users can comment on posts to provide information or ask questions.
    /// </summary>
    public class Comment : BaseEntity
    {
        /// <summary>
        /// Foreign key to the post being commented on.
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// Navigation property to the post.
        /// </summary>
        public Post Post { get; set; } = null!;

        /// <summary>
        /// Foreign key to the user who made the comment.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Navigation property to the user who commented.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// The comment text content.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
