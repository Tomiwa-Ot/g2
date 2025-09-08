using FluentValidation;
using G2.Service.Authentication.Dto.Receiving;

namespace G2.Service.Authentication.Validation
{
    public class PasswordResetValidator: AbstractValidator<ResetPasswordDto>
    {
        public PasswordResetValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid format")
                .NotEmpty().WithMessage("Email is required");

            RuleFor(x => x.ResetToken)
                .NotEmpty().WithMessage("Token is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.Password).WithMessage("Password does not match");
        }
    }
}