using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.User;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, BaseResponse<SafeUserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<SafeUserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if user exists
                var user = await _unitOfWork.Users.GetQueryable()
                    .Where(u => u.Id == request.Id)
                    .Select(u => new SafeUserDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,
                        IsVerified = u.IsVerified,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    return BaseResponse<SafeUserDto>.FailureResult("User not found");
                }

                // Check authorization: Only admins can view other users, or users can view themselves
                if (request.RequestingUserRole != "Admin" && request.RequestingUserId != request.Id)
                {
                    return BaseResponse<SafeUserDto>.FailureResult("Access denied. You can only view your own profile.");
                }

                return BaseResponse<SafeUserDto>.SuccessResult(user, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<SafeUserDto>.FailureResult($"Error retrieving user: {ex.Message}");
            }
        }
    }
}
