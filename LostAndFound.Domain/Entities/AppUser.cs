using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities
{
    public class AppUser : IdentityUser<int>
    {
        public string FullName { get; set; } = string.Empty;

        // Keep Email property from IdentityUser in sync with existing usage
        public bool IsVerified { get; set; } = false;
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeExpiry { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public string? EmailChangeToken { get; set; }
        public DateTime? EmailChangeTokenExpiry { get; set; }
        public string? PendingEmail { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Profile
        public string Phone { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? ProfilePictureUrl { get; set; }

        /// <summary>
        /// Google OAuth subject (sub) claim. Set when user signs in with Google.
        /// </summary>
        public string? GoogleId { get; set; }

        public bool IsBlocked { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    }
}

