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

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new BaseResponse();

            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    response = BaseResponse.FailureResult("Invalid request data", new List<string> { exception.Message });
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case UnauthorizedAccessException:
                    response = BaseResponse.FailureResult("Unauthorized access", new List<string> { exception.Message });
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case KeyNotFoundException:
                    response = BaseResponse.FailureResult("Resource not found", new List<string> { exception.Message });
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                case ValidationException validationEx:
                    var errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
                    response = BaseResponse.FailureResult("Validation failed", errors);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                default:
                    response = BaseResponse.FailureResult("An error occurred while processing your request", new List<string> { exception.Message });
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
