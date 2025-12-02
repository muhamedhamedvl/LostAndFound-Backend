// Code written by Mohamed Hamed Mohamed

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LostAndFound.Domain.Entities
{
    /// <summary>
    /// Represents a post in the Lost and Found system.
    /// A post can be either a lost item report or a found item report.
    /// Contains location information, reward details, and status tracking.
    /// </summary>
    public class Post : BaseEntity
    {
        /// <summary>
        /// The main content/description of the post. Describes the lost or found item.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the subcategory this post belongs to.
        /// Subcategories provide more specific classification than main categories.
        /// </summary>
        public int SubCategoryId { get; set; }
        
        /// <summary>
        /// Navigation property to the subcategory entity.
        /// </summary>
        public SubCategory? SubCategory { get; set; }

        /// <summary>
        /// Current status of the post. Valid values: "Active", "Resolved", "Closed".
        /// Active: Post is still open and searchable.
        /// Resolved: Item has been found/returned.
        /// Closed: Post has been closed by the creator or administrator.
        /// </summary>
        public string Status { get; set; } = "Active"; // "Active", "Resolved", "Closed"
        
        /// <summary>
        /// Timestamp indicating when the post was marked as resolved.
        /// Null if the post has not been resolved yet.
        /// </summary>
        public DateTime? ResolvedAt { get; set; }
        
        /// <summary>
        /// Foreign key to the user who resolved this post (typically the person who found/returned the item).
        /// </summary>
        public int? ResolvedByUserId { get; set; }
        
        /// <summary>
        /// Navigation property to the user who resolved this post.
        /// </summary>
        public User? ResolvedByUser { get; set; } 

        /// <summary>
        /// Foreign key to the user who created this post.
        /// </summary>
        public int CreatorId { get; set; }
        
        /// <summary>
        /// Foreign key to the user who owns the item (if different from creator).
        /// Nullable as the creator may be the owner.
        /// </summary>
        public int? OwnerId { get; set; }
        
        /// <summary>
        /// Latitude coordinate of the location where the item was lost or found.
        /// Used for geospatial searches and mapping.
        /// </summary>
        public double? Latitude { get; set; }
        
        /// <summary>
        /// Longitude coordinate of the location where the item was lost or found.
        /// Used for geospatial searches and mapping.
        /// </summary>
        public double? Longitude { get; set; }
        
        /// <summary>
        /// Human-readable address or location description.
        /// Provides context alongside GPS coordinates.
        /// </summary>
        public string? Address { get; set; }
        
        /// <summary>
        /// Optional reward amount offered for finding/returning the item.
        /// Stored as decimal for precise monetary calculations.
        /// </summary>
        public decimal? RewardAmount { get; set; }
        
        /// <summary>
        /// Platform fee amount deducted from the reward (if applicable).
        /// Used for transaction processing and platform revenue.
        /// </summary>
        public decimal? PlatformFeeAmount { get; set; }

        /// <summary>
        /// Navigation property to the user who created this post.
        /// </summary>
        public User? Creator { get; set; }
        
        /// <summary>
        /// Navigation property to the user who owns the item.
        /// </summary>
        public User? Owner { get; set; }
        
        /// <summary>
        /// Collection of images associated with this post.
        /// Multiple images can be attached to provide visual details of the item.
        /// </summary>
        public ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();
        
        // Photos navigation property - enabled for photo uploads
        public ICollection<Photo> Photos { get; set; } = new List<Photo>();
        //public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        //public ICollection<Like> Likes { get; set; } = new List<Like>();
        //public ICollection<Share> Shares { get; set; } = new List<Share>();
        //public Reward? Reward { get; set; }
    }
}

