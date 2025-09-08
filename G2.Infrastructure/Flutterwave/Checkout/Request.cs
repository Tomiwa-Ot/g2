using G2.Infrastructure.Flutterwave.Http;
using G2.Infrastructure.Flutterwave.Models;

namespace G2.Infrastructure.Flutterwave.Checkout
{
    public class Request : IRequest
    {
        private readonly IApiClient _apiClient;

        public Request(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<CheckoutResponse> MakePayment(Payment payment)
            => await _apiClient.Send<CheckoutResponse>(HttpMethod.Post, "payments", payment);

        public async Task<TransactionVerificationResponse> VerifyTransaction(long id)
            => await _apiClient.Send<TransactionVerificationResponse>(HttpMethod.Get, $"transactions/{id}/verify");
    }
}