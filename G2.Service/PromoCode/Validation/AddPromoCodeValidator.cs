using FluentValidation;
using G2.Service.PromoCode.Dto.Receiving;

namespace G2.Service.PromoCode.Validation
{
    public class AddPromoCodeValidator: AbstractValidator<AddPromoCodeDto>
    {
        public AddPromoCodeValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required");

            RuleFor(x => x.Discount)
                .NotEmpty().WithMessage("Discount is required");

            RuleFor(x => x.UsageLimit)
                .NotEmpty().WithMessage("Usage limit is required");
            
            RuleFor(x => x.ExpiredAt)
                .NotEmpty().WithMessage("Expired at is required");
        }
    }
}