using FluentValidation;
using G2.Service.User.Dto.Receiving;

namespace G2.Service.User.Validation
{
    public class PasswordResetValidator: AbstractValidator<PasswordResetDto>
    {
        public PasswordResetValidator()
        {
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.Password).WithMessage("Password does not match");
        }
    }
}