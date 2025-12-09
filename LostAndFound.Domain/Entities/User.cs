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
        
        /// <summary>
        /// Refresh token used to obtain new access tokens without re-authentication.
        /// Stored as a secure random string.
        /// </summary>
        public string? RefreshToken { get; set; }
        
        /// <summary>
        /// Expiration timestamp for the refresh token. Typically set to 7-30 days from generation.
        /// </summary>
        public DateTime? RefreshTokenExpiry { get; set; }
        
        /// <summary>
        /// Token used for password reset requests. Generated when user requests password reset.
        /// </summary>
        public string? PasswordResetToken { get; set; }
        
        /// <summary>
        /// Expiration timestamp for the password reset token. Typically set to 1 hour from generation.
        /// </summary>
        public DateTime? PasswordResetTokenExpiry { get; set; }

        /// <summary>
        /// Token used for email change requests. Generated when user requests email change.
        /// </summary>
        public string? EmailChangeToken { get; set; }
        
        /// <summary>
        /// Expiration timestamp for the email change token. Typically set to 24 hours from generation.
        /// </summary>
        public DateTime? EmailChangeTokenExpiry { get; set; }
        
        /// <summary>
        /// New email address awaiting verification. User must verify this before it becomes active.
        /// </summary>
        public string? PendingEmail { get; set; }

        /// <summary>
        /// Indicates whether the user account has been soft-deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        /// <summary>
        /// Timestamp when the user account was soft-deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

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
