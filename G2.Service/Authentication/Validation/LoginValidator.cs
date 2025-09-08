using FluentValidation;
using G2.Service.Authentication.Dto.Receiving;

namespace G2.Service.Authentication.Validation
{
    public class LoginValidator: AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid format")
                .NotEmpty().WithMessage("Email is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}