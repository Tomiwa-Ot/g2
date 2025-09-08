using FluentValidation;
using G2.Service.Authentication.Dto.Receiving;
using G2.Service.Helper;

namespace G2.Service.Authentication.Validation
{
    public class GithubValidator: AbstractValidator<GithubDto>
    {
        public GithubValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Token is required");

        }
    }
}