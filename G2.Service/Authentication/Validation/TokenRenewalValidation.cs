using FluentValidation;
using G2.Service.Authentication.Dto.Receiving;

namespace G2.Service.Authentication.Validation
{
    public class TokenRenewalValidator: AbstractValidator<RenewTokenDto>
    {
        public TokenRenewalValidator()
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage("Access token is required");

            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required");
        }
    }
}