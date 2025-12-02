using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.User;
using MediatR;

namespace LostAndFound.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQuery : IRequest<BaseResponse<SafeUserDto>>
    {
        public int Id { get; set; }
        public int RequestingUserId { get; set; }
        public string RequestingUserRole { get; set; } = string.Empty;
    }
}
