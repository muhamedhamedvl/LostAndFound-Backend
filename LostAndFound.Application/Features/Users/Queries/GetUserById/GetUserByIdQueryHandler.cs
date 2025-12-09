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

                // All authenticated users can view profiles (social media requirement)
                // Profile data is already "safe" - no sensitive information exposed

                return BaseResponse<SafeUserDto>.SuccessResult(user, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<SafeUserDto>.FailureResult($"Error retrieving user: {ex.Message}");
            }
        }
    }
}
