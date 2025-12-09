using LostAndFound.Application.Common.Behaviors;
using LostAndFound.Application.Features.Users.Queries.GetUserById;
using LostAndFound.Application.Interfaces;
using LostAndFound.Application.Mapping;
using LostAndFound.Application.Services;
using LostAndFound.Infrastructure.Persistence;
using LostAndFound.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
                    }));

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
            return services;
        }
    }
}
