using FluentValidation;
using LostAndFound.Application.DTOs.Auth;
using LostAndFound.Domain.Enums;

namespace LostAndFound.Application.Validators
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(1).WithMessage("Password cannot be empty");
        }
    }


    public class SignupDtoValidator : AbstractValidator<SignupDto>
    {
        private static readonly string[] ValidGenders = Enum.GetNames<Gender>();

        public SignupDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone is required")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters");

            RuleFor(x => x.Gender)
                .Must(g => g == null || ValidGenders.Contains(g, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Invalid gender. Allowed values: {string.Join(", ", Enum.GetNames<Gender>())}");
        }
    }



    public class VerifyAccountDtoValidator : AbstractValidator<VerifyAccountDto>
    {
        public VerifyAccountDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Verification code is required")
                .Length(6).WithMessage("Verification code must be 6 digits");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
        }
    }

}
