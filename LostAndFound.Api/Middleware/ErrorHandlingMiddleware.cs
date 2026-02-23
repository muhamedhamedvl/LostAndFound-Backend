using LostAndFound.Application.Common;
using FluentValidation;
using System.Net;
using System.Text.Json;
namespace LostAndFound.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex, _environment.IsDevelopment());
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, bool isDevelopment)
        {
            context.Response.ContentType = "application/json";
            
            var response = new BaseResponse();

            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    response = BaseResponse.FailureResult("Invalid request data",
                        isDevelopment ? new List<string> { exception.Message } : null);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case UnauthorizedAccessException:
                    response = BaseResponse.FailureResult("Unauthorized access");
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case KeyNotFoundException:
                    response = BaseResponse.FailureResult("Resource not found");
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                case ValidationException validationEx:
                    var errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
                    response = BaseResponse.FailureResult("Validation failed", errors);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                default:
                    response = BaseResponse.FailureResult(
                        "An error occurred while processing your request",
                        isDevelopment ? new List<string> { exception.Message } : null);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
