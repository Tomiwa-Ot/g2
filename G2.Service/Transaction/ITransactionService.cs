using G2.Infrastructure.Model;
using G2.Service.Transaction.Dto.Receiving;

namespace G2.Service.Transaction
{
    public interface ITransactionService
    {
        Task<Response> AddTransaction(AddTransactionDto addTransactionDto);
        Task<Response> GetTransactionById(long id);
        Task<Response> GetUserTransactions(int page = 1, int limit = 10);
        Task<Response> UpdateErcaspayTransaction(UpdateErcaspayTransactionDto updateTransactionDto);
        Task<Response> UpdateFlutterwaveTransaction(UpdateFlutterwaveTransactionDto updateFlutterwave);
        Task<Response> GetTransactions(int page = 1, int limit = 10, string? query = null);
        Task<Response> VerifyFlutterwave(VerifyFlutterwave verifyFlutterwave);
    }
}