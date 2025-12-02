// Code written by Mohamed Hamed Mohamed

using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents a user in the Lost and Found system.
    /// Contains authentication credentials, profile information, and relationships to other entities.
    /// </summary>
    public class User : BaseEntity
    {
        /// <summary>
        /// User's full name as displayed in the system.
        /// </summary>
        public string FullName { get; set; } = string.Empty;
        
        /// <summary>
        /// User's email address. Used for authentication and communication. Must be unique.
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Hashed password using BCrypt algorithm. Never store plain text passwords.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;
        
        /// <summary>
        /// Indicates whether the user's email account has been verified.
        /// Users must verify their account before they can log in.
        /// </summary>
        public bool IsVerified { get; set; } = false;
        
        /// <summary>
        /// Six-digit verification code sent to the user's email for account verification.
        /// </summary>
        public string? VerificationCode { get; set; }
        
        /// <summary>
        /// Expiration timestamp for the verification code. Typically set to 24 hours from generation.
        /// </summary>
        public DateTime? VerificationCodeExpiry { get; set; }

        // Profile Information (kept for existing features)
        /// <summary>
        /// User's phone number for contact purposes.
        /// </summary>
        public string Phone { get; set; } = string.Empty;
        
        /// <summary>
        /// User's date of birth. Optional field for profile completion.
        /// </summary>
        public DateTime? DateOfBirth { get; set; }
        
        /// <summary>
        /// User's gender. Optional field for profile completion.
        /// </summary>
        public string? Gender { get; set; }
        
        /// <summary>
        /// URL to the user's profile picture. Stored as a reference to the image location.
        /// </summary>
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// Collection of user roles. Defines the user's permissions and access levels in the system.
        /// </summary>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        
        /// <summary>
        /// Collection of posts created by this user. Includes both lost and found items.
        /// </summary>
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        
        // TEMPORARILY DISABLED FOR MVP - Navigation properties commented out to prevent EF Core table creation
        //public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        //public ICollection<Like> Likes { get; set; } = new List<Like>();
        //public ICollection<Share> Shares { get; set; } = new List<Share>();
        
        /// <summary>
        /// Collection of chat sessions where this user is a participant.
        /// Enables real-time communication between users.
        /// </summary>
        public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
        //public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
