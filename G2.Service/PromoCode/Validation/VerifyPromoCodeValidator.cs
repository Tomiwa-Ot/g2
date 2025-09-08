using FluentValidation;
using G2.Service.PromoCode.Dto.Receiving;

namespace G2.Service.PromoCode.Validation
{
    public class VerifyPromoCodeValidator: AbstractValidator<VerifyPromoCodeDto>
    {
        public VerifyPromoCodeValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required");

            RuleFor(x => x.PlanId)
                .NotEmpty().WithMessage("PlanId is required");

            RuleFor(x => x.IsYearly)
                .NotEmpty().WithMessage("IsYearly is required");
        }
    }
}