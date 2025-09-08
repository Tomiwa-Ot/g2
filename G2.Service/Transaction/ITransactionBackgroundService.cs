using G2.Infrastructure.Model;

namespace G2.Service.Transaction
{
    public interface ITransactionBackgroundService
    {
        Task UpdatePendingTransactions();
    }
}