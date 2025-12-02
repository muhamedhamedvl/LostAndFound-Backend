using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using MediatR;

namespace LostAndFound.Application.Features.Users.Commands.CreateAdmin
{
    public class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, BaseResponse<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateAdminCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<UserDto>> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    return BaseResponse<UserDto>.FailureResult("User with this email already exists");
                }
                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    IsVerified = true, 
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsVerified = user.IsVerified,
                    CreatedAt = user.CreatedAt
                };

                return BaseResponse<UserDto>.SuccessResult(userDto, "Admin user created successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<UserDto>.FailureResult($"Failed to create admin user: {ex.Message}");
            }
        }
    }
}
