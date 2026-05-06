using LostAndFound.Infrastructure;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using LostAndFound.Application.Interfaces;
using LostAndFound.Application.Services;
using LostAndFound.Application.Validators;
using LostAndFound.Api.Filters;
using LostAndFound.Api.Hubs;
using LostAndFound.Api.Options;
using LostAndFound.Api.Services;
using FluentValidation;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using LostAndFound.Api.Services.Interfaces;
using LostAndFound.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

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

                // In production, set secrets via environment variables (e.g. Gemini__ApiKey).

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

                    // Include XML comments from the API project for Swagger descriptions
                    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                    if (File.Exists(xmlPath))
                        c.IncludeXmlComments(xmlPath);

                    c.SchemaFilter<SwaggerExcludeFilter>();
                    c.ParameterFilter<FileUploadParameterFilter>();
                    c.OperationFilter<FileUploadOperationFilter>();
                });

                // Infrastructure
                builder.Services.AddInfrastructureServices(builder.Configuration);

                // Gemini embeddings + Modal vector search
                builder.Services.Configure<AiServiceOptions>(builder.Configuration.GetSection(AiServiceOptions.SectionName));
                builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
                builder.Services.Configure<ModalOptions>(builder.Configuration.GetSection(ModalOptions.SectionName));
                builder.Services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();
                builder.Services.AddHttpClient<IModalService, ModalService>();
                builder.Services.AddHttpClient<IAiService, AiService>((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;
                    client.BaseAddress = new Uri(options.GetNormalizedBaseUrl());
                    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds == 0 ? 60 : options.TimeoutSeconds));
                });

                // Firebase options (bind from config / User Secrets)
                builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection(FirebaseOptions.SectionName));

                // Push notifications: Firebase if configured, else stub
                var firebaseOptions = builder.Configuration.GetSection(FirebaseOptions.SectionName).Get<FirebaseOptions>();
                if (firebaseOptions?.IsValid == true)
                {
                    try
                    {
                        var json = FirebaseCredentialHelper.BuildServiceAccountJson(
                            firebaseOptions.ProjectId,
                            firebaseOptions.ClientEmail,
                            firebaseOptions.PrivateKey);
                        var credential = GoogleCredential.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                        if (FirebaseApp.DefaultInstance == null)
                            FirebaseApp.Create(new AppOptions { Credential = credential });
                        builder.Services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
                        Log.Information("Firebase push notifications enabled.");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Firebase init failed; using stub push service.");
                        builder.Services.AddScoped<IPushNotificationService, PushNotificationServiceStub>();
                    }
                }
                else
                {
                    builder.Services.AddScoped<IPushNotificationService, PushNotificationServiceStub>();
                }

                // Image Service
                builder.Services.AddScoped<IImageService>(sp =>
                    new ImageService(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

                // Validators
                builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

                // JWT Authentication - validate required config
                var jwtSettings = builder.Configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        secretKey = "DevOnly-32CharsMin-LostAndFound-Local-Key";
                        Log.Warning("Using default dev JWT secret. Set JwtSettings:SecretKey in User Secrets for production-like setup.");
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "JwtSettings:SecretKey is required and must be at least 32 characters in Production. " +
                            "Set JwtSettings__SecretKey environment variable.");
                    }
                }

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
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                            ClockSkew = TimeSpan.Zero
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
                builder.Services.AddScoped<IRealtimeNotificationSender, SignalRNotificationSender>();

                // CORS - production: set Cors:AllowedOrigins (JSON array) or Cors__AllowedOrigins (comma-separated); dev: AllowAll if empty
                var originsFromArray = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                var originsFromEnv = builder.Configuration["Cors__AllowedOrigins"];
                var allowedOrigins = (originsFromArray?.Length > 0 ? originsFromArray : null)
                    ?? (string.IsNullOrEmpty(originsFromEnv) ? Array.Empty<string>() : originsFromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                builder.Services.AddCors(options =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        options.AddPolicy("Configured", policy =>
                        {
                            policy.WithOrigins(allowedOrigins)
                                  .AllowAnyHeader()
                                  .AllowAnyMethod()
                                  .AllowCredentials();
                        });
                    }
                    else if (builder.Environment.IsDevelopment())
                    {
                        // Development only: allow any origin for SignalR compatibility.
                        // SignalR requires AllowCredentials which is incompatible with AllowAnyOrigin.
                        options.AddPolicy("Configured", policy =>
                        {
                            policy.SetIsOriginAllowed(_ => true)
                                  .AllowAnyHeader()
                                  .AllowAnyMethod()
                                  .AllowCredentials();
                        });
                    }
                    else
                    {
                        // Production with no configured origins: reject all cross-origin requests.
                        Log.Warning("No CORS origins configured for Production. All cross-origin requests will be rejected.");
                        options.AddPolicy("Configured", policy =>
                        {
                            policy.SetIsOriginAllowed(_ => false)
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();
                        });
                    }
                });

                // Rate Limiting
                builder.Services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    // Auth endpoints: 5 requests per minute per IP
                    options.AddFixedWindowLimiter("auth", opt =>
                    {
                        opt.PermitLimit = 5;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 0;
                    });

                    // Refresh-token: separate higher limit since mobile apps refresh frequently
                    options.AddFixedWindowLimiter("refresh", opt =>
                    {
                        opt.PermitLimit = 20;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 0;
                    });

                    // Upload endpoints: 10 requests per minute per IP
                    options.AddFixedWindowLimiter("upload", opt =>
                    {
                        opt.PermitLimit = 10;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 0;
                    });

                    // General API: 100 requests per minute per IP
                    options.AddFixedWindowLimiter("api", opt =>
                    {
                        opt.PermitLimit = 100;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 2;
                    });
                });

                var app = builder.Build();

                // Seed Admin + Roles
                using (var scope = app.Services.CreateScope())
                {
                    try
                    {
                        await LostAndFound.Infrastructure.Persistence.DbSeeder.SeedAsync(scope.ServiceProvider);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "DbSeeder failed at startup. Continuing app boot so Swagger/API remain reachable.");
                    }
                }

                // Swagger security in Production (IP whitelist + Basic Auth)
                if (app.Environment.IsProduction())
                {
                    var swaggerUser = Environment.GetEnvironmentVariable("SWAGGER_USER");
                    var swaggerPass = Environment.GetEnvironmentVariable("SWAGGER_PASS");
                    var swaggerIps = Environment.GetEnvironmentVariable("SWAGGER_ALLOWED_IPS");

                    var allowedIpSet = new HashSet<IPAddress>();
                    if (!string.IsNullOrWhiteSpace(swaggerIps))
                    {
                        foreach (var ipStr in swaggerIps.Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            if (IPAddress.TryParse(ipStr, out var ip))
                            {
                                allowedIpSet.Add(ip);
                            }
                        }
                    }

                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
                        {
                            // IP whitelist (if configured)
                            if (allowedIpSet.Count > 0)
                            {
                                var remoteIp = context.Connection.RemoteIpAddress;
                                var ipToCheck = remoteIp?.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                                    ? remoteIp.MapToIPv4()
                                    : remoteIp;

                                if (ipToCheck == null || !allowedIpSet.Contains(ipToCheck))
                                {
                                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                    await context.Response.WriteAsync("Forbidden");
                                    return;
                                }
                            }

                            // Basic Auth (if credentials are configured)
                            if (!string.IsNullOrEmpty(swaggerUser) && !string.IsNullOrEmpty(swaggerPass))
                            {
                                if (!context.Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
                                {
                                    context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    await context.Response.WriteAsync("Unauthorized");
                                    return;
                                }

                                var authHeader = authHeaderValues.ToString();
                                if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                                {
                                    context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    await context.Response.WriteAsync("Unauthorized");
                                    return;
                                }

                                string decoded;
                                try
                                {
                                    var encoded = authHeader.Substring("Basic ".Length).Trim();
                                    decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                                }
                                catch
                                {
                                    context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    await context.Response.WriteAsync("Unauthorized");
                                    return;
                                }

                                var parts = decoded.Split(':', 2);
                                if (parts.Length != 2 ||
                                    !string.Equals(parts[0], swaggerUser, StringComparison.Ordinal) ||
                                    !string.Equals(parts[1], swaggerPass, StringComparison.Ordinal))
                                {
                                    context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    await context.Response.WriteAsync("Unauthorized");
                                    return;
                                }
                            }
                        }

                        await next();
                    });
                }

                // Swagger: enabled in all environments now
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                    c.RoutePrefix = "swagger";
                });

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseCors("Configured");

                // Rate Limiting
                app.UseRateLimiter();

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
