using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Auth;
using MediatR;

namespace LostAndFound.Application.Features.Users.Commands.CreateAdmin
{
    public class CreateAdminCommand : IRequest<BaseResponse<UserDto>>
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
