using System.Data;
using FluentValidation;
using G2.Service.Plan.Dto.Receiving;

namespace G2.Service.Plan.Validation
{
    public class AddPlanValidator: AbstractValidator<AddPlanDto>
    {
        public AddPlanValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required");

            RuleFor(x => x.Price)
                .NotEmpty().WithMessage("Price is required")
                .GreaterThanOrEqualTo(0).WithMessage("Price cannot be less than 0");

            RuleFor(x => x.Discount)
                .NotEmpty().WithMessage("Discount is required")
                .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be less than 0")
                .LessThanOrEqualTo(100).WithMessage("Discount cannot be greater than 100");
        }
    }
}