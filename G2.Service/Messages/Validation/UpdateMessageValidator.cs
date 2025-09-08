using FluentValidation;
using G2.Service.Messages.Dto.Receiving;

namespace G2.Service.Messages.Validation
{
    public class UpdateMessageValidator: AbstractValidator<UpdateMessageDto>
    {
        public UpdateMessageValidator()
        {
            RuleFor(x => x.Unread)
                .NotEmpty().WithMessage("Unread is required");
        }
    }
}