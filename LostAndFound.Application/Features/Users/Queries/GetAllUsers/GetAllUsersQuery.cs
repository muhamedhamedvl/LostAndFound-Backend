using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Common;
using LostAndFound.Application.DTOs.User;
using MediatR;

namespace LostAndFound.Application.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersQuery : IRequest<BaseResponse<PaginatedResponse<SafeUserDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }
}
