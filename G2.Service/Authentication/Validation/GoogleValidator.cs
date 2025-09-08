using FluentValidation;
using G2.Service.Authentication.Dto.Receiving;
using G2.Service.Helper;

namespace G2.Service.Authentication.Validation
{
    public class GoogleValidator: AbstractValidator<GoogleDto>
    {
        public GoogleValidator()
        {
            // RuleFor(x => x.Code)
            //     .NotEmpty().WithMessage("Token is required");

        }
    }
}