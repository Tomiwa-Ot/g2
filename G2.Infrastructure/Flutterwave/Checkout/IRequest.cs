using G2.Infrastructure.Flutterwave.Models;

namespace G2.Infrastructure.Flutterwave.Checkout
{
    public interface IRequest
    {
        Task<CheckoutResponse> MakePayment(Payment payment);
        Task<TransactionVerificationResponse> VerifyTransaction(long id);
    }
}