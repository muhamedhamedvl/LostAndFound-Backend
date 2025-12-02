// Code written by Mohamed Hamed Mohamed

using AutoMapper;
using LostAndFound.Application.DTOs.Category;
using LostAndFound.Application.DTOs.Chat;
using LostAndFound.Application.DTOs.Item;
using LostAndFound.Application.DTOs.Post;
using LostAndFound.Application.DTOs.Location;
using LostAndFound.Application.DTOs.Notification;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace LostAndFound.Application.Mapping
{
    /// <summary>
    /// AutoMapper profile for configuring entity-to-DTO mappings.
    /// Defines all mapping configurations between domain entities and DTOs.
    /// </summary>
    public class MappingProfile : Profile
    {
        /// <summary>
        /// Initializes mapping configurations for all entities and DTOs.
        /// Configures bidirectional mappings where applicable.
        /// </summary>
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles != null && src.UserRoles.Any()
                    ? src.UserRoles.Select(ur => ur.Role != null ? ur.Role.Name : string.Empty).Where(r => !string.IsNullOrEmpty(r)).ToList()
                    : new List<string>()))
                .ForMember(dest => dest.ProfilePictureUrl, opt => opt.MapFrom(src => src.ProfilePictureUrl))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender));
            CreateMap<SignupDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false));

            // Category mappings
            // CategoryDto: Flat DTO without SubCategories (for lightweight list endpoints)
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.SubCategoryCount, opt => opt.MapFrom(src => 
                    src.SubCategories != null && src.SubCategories.Any() ? src.SubCategories.Count : 0));
            
            // CategoryTreeDto: Full hierarchical DTO with SubCategories (for tree endpoints)
            CreateMap<Category, CategoryTreeDto>()
                .ForMember(dest => dest.SubCategories, opt => opt.MapFrom(src => src.SubCategories));
            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore());
            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore());

            // SubCategory mappings
            CreateMap<SubCategory, SubCategoryDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.Posts.Count));
            CreateMap<CreateSubCategoryDto, SubCategory>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Posts, opt => opt.Ignore());
            CreateMap<UpdateSubCategoryDto, SubCategory>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Posts, opt => opt.Ignore());

            // Location mappings
            CreateMap<Location, LocationDto>();
            CreateMap<CreateLocationDto, Location>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            CreateMap<UpdateLocationDto, Location>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Post mappings
            CreateMap<Post, PostDto>()
                .ForMember(dest => dest.SubCategory, opt => opt.MapFrom(src => src.SubCategory))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.SubCategory != null ? src.SubCategory.CategoryId : 0))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.SubCategory != null && src.SubCategory.Category != null ? src.SubCategory.Category.Name : string.Empty))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.PostImages != null ? src.PostImages.Select(pi => pi.ImageUrl) : Enumerable.Empty<string>()))
                .ForMember(dest => dest.Reward, opt => opt.MapFrom(src => src.RewardAmount.HasValue
                    ? src.RewardAmount.Value - (src.PlatformFeeAmount ?? 0m)
                    : (decimal?)null))
                // Map Photos from Post entity to PostDto
                .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos != null && src.Photos.Any() 
                    ? src.Photos.Select(p => new PhotoDto
                    {
                        Id = p.Id,
                        Url = p.Url,
                        PublicId = p.PublicId,
                        PostId = p.PostId,
                        UploadedAt = p.UploadedAt
                    }).ToList()
                    : new List<PhotoDto>()));


            CreateMap<CreatePostDto, Post>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Active"))
                .ForMember(dest => dest.CreatorId, opt => opt.Ignore()) 
                .ForMember(dest => dest.SubCategory, opt => opt.Ignore())
                .ForMember(dest => dest.Creator, opt => opt.Ignore())
                .ForMember(dest => dest.RewardAmount, opt => opt.Ignore())
                .ForMember(dest => dest.PlatformFeeAmount, opt => opt.Ignore())
                .ForMember(dest => dest.Photos, opt => opt.Ignore()) 
                .ForMember(dest => dest.PostImages, opt => opt.Ignore()) 
                .ForMember(dest => dest.ResolvedByUser, opt => opt.Ignore());

            CreateMap<UpdatePostDto, Post>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
                .ForMember(dest => dest.RewardAmount, opt => opt.Ignore())
                .ForMember(dest => dest.PlatformFeeAmount, opt => opt.Ignore());

            // Photo mappings
            CreateMap<Photo, PhotoDto>();
            CreateMap<PhotoDto, Photo>();

            // Reward mappings
            CreateMap<Reward, DTOs.Item.RewardDto>();
            CreateMap<DTOs.Item.RewardDto, Reward>();

            // Chat Session mappings
            CreateMap<ChatSession, ChatSessionDetailsDto>();

            // Chat Message mappings
            CreateMap<ChatMessage, ChatMessageDto>();
        }
    }
}
