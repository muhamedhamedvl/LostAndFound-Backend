using LostAndFound.Api.Models.SignalR;
using LostAndFound.Application.DTOs.Chat;
using System.Threading.Tasks;

namespace LostAndFound.Api.Services.Interfaces
{
    public interface IChatHubService
    {
        Task NotifyMessageSentAsync(ChatMessageDto message);
        Task NotifyUserTypingAsync(TypingIndicatorPayload payload);
        Task NotifyMessageReadAsync(ChatMessageDto message);
        Task NotifySessionCreatedAsync(SessionCreatedPayload payload);
        Task NotifyPresenceChangedAsync(PresenceStatusPayload payload);
    }
}

