using System.Linq;
using System.Security.Claims;
using LostAndFound.Api.Models.SignalR;
using LostAndFound.Api.Services.Interfaces;
using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Chat;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
namespace LostAndFound.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IChatHubService _chatHubService;

        public ChatController(IChatService chatService, IChatHubService chatHubService)
        {
            _chatService = chatService;
            _chatHubService = chatHubService;
        }

        [HttpGet("sessions")]
        [SwaggerOperation(
            Summary = "Get chat sessions",
            Description = "Retrieves all chat sessions for the authenticated user. Returns a list of chat sessions with other users. Requires authentication."
        )]
        public async Task<IActionResult> GetSessionsAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                var sessions = await _chatService.GetUserSessionsAsync(userId);
                return Ok(BaseResponse<IEnumerable<ChatSessionSummaryDto>>.SuccessResult(sessions, "Chat sessions retrieved successfully."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Failed to load chat sessions: {ex.Message}"));
            }
        }

        [HttpPost("sessions/{otherUserId:int}")]
        [SwaggerOperation(
            Summary = "Open or create chat session",
            Description = "Opens an existing chat session or creates a new one with another user. Returns the chat session details. Requires authentication."
        )]
        public async Task<IActionResult> OpenSessionAsync(int otherUserId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _chatService.OpenOrCreateSessionAsync(userId, otherUserId);
                var targetUserId = session.User1Id == userId ? session.User2Id : session.User1Id;
                if (targetUserId > 0)
                {
                    await _chatHubService.NotifySessionCreatedAsync(new SessionCreatedPayload
                    {
                        SessionId = session.Id,
                        InitiatorUserId = userId,
                        TargetUserId = targetUserId,
                        Session = session
                    });
                }
                return Ok(BaseResponse<ChatSessionDetailsDto>.SuccessResult(session, "Chat session ready."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Failed to open chat session: {ex.Message}"));
            }
        }

        [HttpGet("sessions/{sessionId:int}")]
        [SwaggerOperation(
            Summary = "Get chat session details",
            Description = "Retrieves detailed information about a specific chat session including participants. Only participants can access the session. Requires authentication."
        )]
        public async Task<IActionResult> GetSessionDetailsAsync(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _chatService.GetSessionDetailsAsync(sessionId, userId);
                return Ok(BaseResponse<ChatSessionDetailsDto>.SuccessResult(session, "Chat session loaded."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Failed to load chat session: {ex.Message}"));
            }
        }

        [HttpGet("sessions/{sessionId:int}/messages")]
        [SwaggerOperation(
            Summary = "Get chat messages",
            Description = "Retrieves all messages in a specific chat session. Only participants can access messages. Requires authentication."
        )]
        public async Task<IActionResult> GetMessagesAsync(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var messages = await _chatService.GetMessagesAsync(sessionId, userId);
                return Ok(BaseResponse<IEnumerable<ChatMessageDto>>.SuccessResult(messages, "Messages loaded successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Failed to load messages: {ex.Message}"));
            }
        }

        [HttpPost("sessions/{sessionId:int}/messages")]
        [SwaggerOperation(
            Summary = "Send chat message",
            Description = "Sends a new message in a chat session. Only participants can send messages. The message is delivered via SignalR in real-time. Requires authentication."
        )]
        public async Task<IActionResult> SendMessageAsync(int sessionId, [FromBody] SendChatMessageDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(BaseResponse<object>.FailureResult("Request body is required."));
                }

                var userId = GetCurrentUserId();
                var messages = await _chatService.SendMessageAsync(sessionId, userId, request.Text);
                var latestMessage = messages.LastOrDefault();

                if (latestMessage != null)
                {
                    await _chatHubService.NotifyMessageSentAsync(latestMessage);
                }

                return Ok(BaseResponse<IEnumerable<ChatMessageDto>>.SuccessResult(messages, "Message sent successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Failed to send message: {ex.Message}"));
            }
        }

        [HttpPut("messages/{messageId:int}/read")]
        [SwaggerOperation(
            Summary = "Mark message as read",
            Description = "Marks a chat message as read. Only the message recipient can mark messages as read. Requires authentication."
        )]
        public async Task<IActionResult> MarkMessageAsReadAsync(int messageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _chatService.MarkMessageAsReadAsync(messageId, userId);
                await _chatHubService.NotifyMessageReadAsync(message);
                return Ok(BaseResponse<ChatMessageDto>.SuccessResult(message, "Message marked as read."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.FailureResult($"Failed to update message: {ex.Message}"));
            }
        }

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdValue, out var userId) || userId <= 0)
            {
                throw new UnauthorizedAccessException("Invalid user token.");
            }

            return userId;
        }
    }
}
