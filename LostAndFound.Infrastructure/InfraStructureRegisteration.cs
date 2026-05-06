using LostAndFound.Application.Common.Behaviors;
using LostAndFound.Application.Features.Users.Queries.GetUserById;
using LostAndFound.Application.Interfaces;
using LostAndFound.Application.Mapping;
using LostAndFound.Application.Services;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Persistence;
using LostAndFound.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LostAndFound.Infrastructure
{
    public static class InfraStructureRegisteration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    })
                // Suppress EF warning about global query filter on AppUser (soft delete)
                // affecting required relationships. This is intentional — deleted users'
                // related data (messages, reports, etc.) remains in the DB but the User
                // navigation may resolve to null, which the application code already handles.
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

            // ASP.NET Core Identity configuration (AppUser only; roles are handled via existing Role/UserRole model and JWT claims)
            services.AddIdentityCore<AppUser>(options =>
                {
                    options.SignIn.RequireConfirmedEmail = true;

                    // Lockout: 5 failed attempts, 15 minutes lockout
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.AllowedForNewUsers = true;

                    // Strong password rules
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequiredUniqueChars = 1;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetUserByIdQueryHandler).Assembly));

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IMatchingService, MatchingService>();
            services.AddScoped<IReportAbuseService, ReportAbuseService>();
            services.AddScoped<ISavedReportService, SavedReportService>();
            services.AddScoped<IHomeService, HomeService>();
            services.AddScoped<IDeviceTokenService, DeviceTokenService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            // IPushNotificationService is registered in Api Program.cs (Firebase or Stub)

            return services;
        }
    }
}
