using FluentValidation;
using G2.Service.Authentication.Dto.Receiving;

namespace G2.Service.Authentication.Validation
{
    public class ForgotPasswordValidator: AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid format")
                .NotEmpty().WithMessage("Email is required");
        }
    }
}