using FluentValidation;
using G2.Service.Authentication.Dto.Receiving;

namespace G2.Service.Authentication.Validation
{
    public class RegistrationValidator: AbstractValidator<RegisterDto>
    {
        public RegistrationValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid format")
                .NotEmpty().WithMessage("Email is required");

            RuleFor(x => x.Fullname)
                .NotEmpty().WithMessage("Fullname is required")
                .Matches("^[A-Za-z ]+$").WithMessage("Name should only contain letters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}