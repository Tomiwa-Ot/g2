using FluentValidation;
using G2.Service.Messages.Dto.Receiving;

namespace G2.Service.Messages.Validation
{
    public class AddMessageValidator: AbstractValidator<AddMessageDto>
    {
        public AddMessageValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Type is required");

            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Body is required");
        }
    }
}