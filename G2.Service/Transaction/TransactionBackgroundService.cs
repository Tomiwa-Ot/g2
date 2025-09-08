using ErcasPay.Services.TransactionService.Response;
using G2.Infrastructure.Flutterwave.Checkout;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.Transaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace G2.Service.Transaction
{
    public class TransactionBackgroundService : BackgroundService, ITransactionBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TransactionBackgroundService> _logger;
        
        public TransactionBackgroundService(IServiceProvider serviceProvider,
                ILogger<TransactionBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task UpdatePendingTransactions()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                ITransactionRepository transactionRepository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
                IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                IRequest flutterwave = scope.ServiceProvider.GetRequiredService<IRequest>();
                ErcasPay.Services.TransactionService.ITransactionService ercasPay =
                    scope.ServiceProvider.GetRequiredService<ErcasPay.Services.TransactionService.ITransactionService>();

                try
                {
                    // fetch all incomplete transactions greater than an hour
                    List<Infrastructure.Model.Transaction> incompleteTransactions = await transactionRepository.FindListAsync(
                        x => !x.IsDeleted && x.CreatedAt.AddHours(1) >= DateTime.UtcNow.AddHours(1) && 
                            x.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)
                    );

                    await unitOfWork.BeginTransactionAsync();
                    
                    foreach (Infrastructure.Model.Transaction transaction in incompleteTransactions)
                    {
                        if (transaction.Provider.Equals("ercaspay", StringComparison.OrdinalIgnoreCase))
                        {
                            VerifyTransactionResponse response = await ercasPay.VerifyTransaction(transaction.ProviderReference);
                            if (response.RequestSuccessful)
                            {
                                transaction.Status = response.ResponseBody.Status;
                            }
                        }
                        else if (transaction.Provider.Equals("flutterwave", StringComparison.OrdinalIgnoreCase))
                        {
                            // flutterwave.VerifyTransaction()
                        }
                        else
                        {
                            transaction.Status = "Failed";
                        }
                        transaction.UpdatedAt = DateTime.UtcNow.AddHours(1);
                        transactionRepository.Update(transaction);
                        await unitOfWork.SaveChangesAsync();
                    }

                    await unitOfWork.CommitTransactionAsync();
                }
                catch (Exception e)
                {
                    await unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(e.Message, e);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                await UpdatePendingTransactions();
            }
        }
    }
}