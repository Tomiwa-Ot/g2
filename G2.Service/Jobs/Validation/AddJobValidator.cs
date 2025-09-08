using FluentValidation;
using G2.Service.Helper;
using G2.Service.Jobs.Dto.Receiving;

namespace G2.Service.Jobs.Validation
{
    public class AddJobValidator: AbstractValidator<AddJobDto>
    {
        public AddJobValidator()
        {
            RuleFor(x => x.Url)
                .Must(VerifyUrl).WithMessage("Invalid url")
                .MustAsync(IsUrlSafe).WithMessage("Invalid url")
                .NotEmpty().WithMessage("Url is required");
        }

        private bool VerifyUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || 
                uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private async Task<bool> IsUrlSafe(string url, CancellationToken cancellationToken)
        {
            return await SSRFChecker.IsUrlSafe(url);
        }
    }
}