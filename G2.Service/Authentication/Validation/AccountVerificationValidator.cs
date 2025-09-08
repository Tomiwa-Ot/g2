using FluentValidation;
using G2.Service.Authentication.Dto.Receiving;

namespace G2.Service.Authentication.Validation
{
    public class AccountVerificationValidator: AbstractValidator<VerifyAccountDto>
    {
        public AccountVerificationValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid format")
                .NotEmpty().WithMessage("Email is required");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required");
        }
    }
}