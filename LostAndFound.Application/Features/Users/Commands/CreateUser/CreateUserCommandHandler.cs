using LostAndFound.Application.Common;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using MediatR;

namespace LostAndFound.Application.Features.Users.Commands.CreateUser
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, BaseResponse<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateUserCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
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
                    IsVerified = false,
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
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return BaseResponse<UserDto>.SuccessResult(userDto, "User created successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<UserDto>.FailureResult($"Error creating user: {ex.Message}");
            }
        }
    }
}
