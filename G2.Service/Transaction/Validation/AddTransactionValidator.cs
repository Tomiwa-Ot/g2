using System.Globalization;
using FluentValidation;
using G2.Infrastructure.Repository.Database.Plan;
using G2.Service.Transaction.Dto.Receiving;

namespace G2.Service.Transaction.Validation
{
    public class AddTransactionValidator: AbstractValidator<AddTransactionDto>
    {
        private readonly IPlanRepository _planRepository;
        public AddTransactionValidator(IPlanRepository planRepository)
        {
            _planRepository = planRepository;

            RuleFor(x => x.PlanId)
                .MustAsync(PlanExists).WithMessage("Invalid plan")
                .NotEmpty().WithMessage("PlanId is required");

            RuleFor(x => x.Yearly)
                .NotNull().WithMessage("Yearly is required");

            RuleFor(x => x.Provider)
                .Must(IsProviderEnabled).WithMessage("Provider is not supported")
                .NotEmpty().WithMessage("Provider is required");
        }

        private async Task<bool> PlanExists(long id, CancellationToken cancellationToken)
        {
            var plan = await _planRepository.FirstOrDefaultAsync(x =>
                x.Id == id && !x.IsDeleted);
            return plan != null;
        }

        public bool IsProviderEnabled(string provider) 
            => provider.Equals("flutterwave", StringComparison.OrdinalIgnoreCase) ||
                provider.Equals("ercaspay", StringComparison.OrdinalIgnoreCase);

        private bool OnlyDigits(string numbers) => numbers.All(char.IsDigit);

        private bool IsCvvValid(string cvv) => OnlyDigits(cvv) && (cvv.Length == 3);

        private bool IsDateValid(string expirayDate) => DateTime.TryParseExact(
            expirayDate, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        
    }
}
