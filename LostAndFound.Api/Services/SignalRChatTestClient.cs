using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LostAndFound.Api.Services
{
    public static class SignalRChatTestClient
    {
        public static async Task<HubConnection> StartAsync(
            string hubUrl,
            int userId,
            string? accessToken = null,
            ILogger? logger = null)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"{hubUrl}/chatHub", options =>
                {
                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(accessToken)!;
                    }
                })
                .WithAutomaticReconnect()
                .Build();

            connection.On<object>("ReceiveMessage", payload =>
            {
                logger?.LogInformation("TestClient[{UserId}] ReceiveMessage: {Payload}", userId, payload);
            });

            connection.On<object>("UserTyping", payload =>
            {
                logger?.LogInformation("TestClient[{UserId}] UserTyping: {Payload}", userId, payload);
            });

            connection.On<object>("MessageRead", payload =>
            {
                logger?.LogInformation("TestClient[{UserId}] MessageRead: {Payload}", userId, payload);
            });

            await connection.StartAsync();
            await connection.InvokeAsync("RegisterUser", userId);

            return connection;
        }
    }
}

