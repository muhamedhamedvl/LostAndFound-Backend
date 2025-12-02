using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Common;
using LostAndFound.Application.DTOs.User;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, BaseResponse<PaginatedResponse<SafeUserDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllUsersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<PaginatedResponse<SafeUserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate pagination parameters
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

                // Build query with search filter
                var query = _unitOfWork.Users.GetQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(u => 
                        u.FullName.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm) ||
                        u.Phone.Contains(searchTerm));
                }

                // Get total count
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply pagination
                var users = await query
                    .OrderBy(u => u.FullName)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
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
                    .ToListAsync(cancellationToken);

                var paginatedResponse = new PaginatedResponse<SafeUserDto>
                {
                    Data = users,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return BaseResponse<PaginatedResponse<SafeUserDto>>.SuccessResult(paginatedResponse, "Users retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<PaginatedResponse<SafeUserDto>>.FailureResult($"Error retrieving users: {ex.Message}");
            }
        }
    }
}
