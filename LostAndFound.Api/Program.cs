using LostAndFound.Infrastructure;
using LostAndFound.Application.Interfaces;
using LostAndFound.Application.Services;
using LostAndFound.Application.Validators;
using LostAndFound.Api.Filters;
using LostAndFound.Api.Hubs;
using LostAndFound.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using LostAndFound.Api.Services.Interfaces;

namespace LostAndFound.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Serilog setup
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/lostandfound-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting LostAndFound API");
                var builder = WebApplication.CreateBuilder(args);

                // Controllers
                builder.Services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                        options.JsonSerializerOptions.WriteIndented = true;
                    });

                // Swagger
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LostAndFound API", Version = "v1" });

                    c.EnableAnnotations();

                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "Authorization: Bearer {token}",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });

                    c.SchemaFilter<SwaggerExcludeFilter>();
                    c.ParameterFilter<FileUploadParameterFilter>();
                    c.OperationFilter<FileUploadOperationFilter>();
                });

                // Infrastructure
                builder.Services.AddInfrastructureServices(builder.Configuration);

                // Validators
                builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

                // JWT Authentication
                var jwtSettings = builder.Configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];

                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtSettings["Issuer"],
                            ValidAudience = jwtSettings["Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
                        };

                        // SignalR JWT support
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var accessToken = context.Request.Query["access_token"];
                                var path = context.HttpContext.Request.Path;

                                if (!string.IsNullOrEmpty(accessToken) &&
                                   (path.StartsWithSegments("/notificationHub") || path.StartsWithSegments("/chatHub")))
                                {
                                    context.Token = accessToken;
                                }

                                return Task.CompletedTask;
                            }
                        };
                    });

                builder.Services.AddAuthorization();

                // Health checks
                builder.Services.AddHealthChecks()
                    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
                    .AddDbContextCheck<LostAndFound.Infrastructure.Persistence.AppDbContext>("database");

                // SignalR
                builder.Services.AddSignalR();
                builder.Services.AddSingleton<IUserConnectionManager, UserConnectionManager>();
                builder.Services.AddScoped<IChatHubService, ChatHubService>();
                builder.Services.AddScoped<INotificationHubService, NotificationHubService>();

                // CORS (Production safe)
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
                });

                var app = builder.Build();

                // Seed Admin + Roles
                using (var scope = app.Services.CreateScope())
                {
                    await LostAndFound.Infrastructure.Persistence.DbSeeder.SeedAsync(scope.ServiceProvider);
                }

                // Swagger ALWAYS enabled (Fix MonsterASP 404)
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LostAndFound API v1");
                    c.RoutePrefix = string.Empty;   // Homepage = Swagger
                });

                app.UseHttpsRedirection();
                app.UseCors("AllowAll");

                // Middlewares
                app.UseMiddleware<LostAndFound.Api.Middleware.RequestLoggingMiddleware>();
                app.UseMiddleware<LostAndFound.Api.Middleware.ErrorHandlingMiddleware>();

                app.UseAuthentication();
                app.UseAuthorization();

                // Routes
                app.MapControllers();
                app.MapHub<ChatHub>("/chatHub");
                app.MapHub<NotificationHub>("/notificationHub");

                // Health endpoints
                app.MapHealthChecks("/health");
                app.MapHealthChecks("/health/ready");
                app.MapHealthChecks("/health/live");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Startup Failure");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
