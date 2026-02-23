using AutoMapper;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.DTOs.Category;
using LostAndFound.Application.DTOs.Chat;
using LostAndFound.Application.DTOs.Notification;
using LostAndFound.Application.DTOs.Report;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace LostAndFound.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ── User mappings ──
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles != null && src.UserRoles.Any()
                    ? src.UserRoles.Select(ur => ur.Role != null ? ur.Role.Name : string.Empty).Where(r => !string.IsNullOrEmpty(r)).ToList()
                    : new List<string>()))
                .ForMember(dest => dest.ProfilePictureUrl, opt => opt.MapFrom(src => src.ProfilePictureUrl))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender));
            CreateMap<SignupDto, User>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName.Trim()} {src.LastName.Trim()}".Trim()))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender));

            // ── Category mappings ──
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.SubCategoryCount, opt => opt.MapFrom(src =>
                    src.SubCategories != null && src.SubCategories.Any() ? src.SubCategories.Count : 0));
            CreateMap<Category, CategoryTreeDto>()
                .ForMember(dest => dest.SubCategories, opt => opt.MapFrom(src => src.SubCategories));
            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore());
            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore());

            // ── SubCategory mappings ──
            CreateMap<SubCategory, SubCategoryDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.ReportCount, opt => opt.MapFrom(src => src.Reports.Count));
            CreateMap<CreateSubCategoryDto, SubCategory>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Reports, opt => opt.Ignore());
            CreateMap<UpdateSubCategoryDto, SubCategory>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Reports, opt => opt.Ignore());

            // ── Report mappings ──
            CreateMap<Report, ReportDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.LifecycleStatus, opt => opt.MapFrom(src => src.LifecycleStatus.ToString()))
                .ForMember(dest => dest.SubCategoryName, opt => opt.MapFrom(src => src.SubCategory != null ? src.SubCategory.Name : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.SubCategory != null && src.SubCategory.Category != null ? src.SubCategory.Category.Name : null))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.FullName : null))
                .ForMember(dest => dest.CreatedByProfilePictureUrl, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.ProfilePictureUrl : null))
                .ForMember(dest => dest.DateReported, opt => opt.MapFrom(src => src.DateReported))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images));

            CreateMap<CreateReportDto, Report>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<ReportType>(src.Type, true)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ReportStatus.Open))
                .ForMember(dest => dest.DateReported, opt => opt.MapFrom(src => src.DateReported))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.SubCategory, opt => opt.Ignore())
                .ForMember(dest => dest.Images, opt => opt.Ignore());

            CreateMap<ReportImage, ReportImageDto>();

            // ── Chat Session mappings ──
            CreateMap<ChatSession, ChatSessionDetailsDto>();
            CreateMap<ChatMessage, ChatMessageDto>();

            // ── Notification mappings ──
            CreateMap<Notification, NotificationDto>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Title) ? src.Title : src.Type))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.NotificationType, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.ActorName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.FullName : null))
                .ForMember(dest => dest.ActorProfilePictureUrl, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.ProfilePictureUrl : null))
                .ForMember(dest => dest.RelatedReportId, opt => opt.MapFrom(src => src.ReportId));
        }
    }
}
