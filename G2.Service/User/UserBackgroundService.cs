using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace G2.Service.User
{
    public class UserBackgroundService : BackgroundService, IUserBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserBackgroundService> _logger;
        
        public UserBackgroundService(IServiceProvider serviceProvider,
                ILogger<UserBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task DowngradePlan()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                try
                {
                    await unitOfWork.BeginTransactionAsync();

                    // Downgrade expired plans to free plan
                    await userRepository.UpdateRangeAsync(x =>
                        x.PlanExpiration.HasValue && DateTime.Today > x.PlanExpiration && x.PlanId != 1,
                        x => x.SetProperty(u => u.PlanId, 1)
                                .SetProperty(u => u.PlanExpiration, (DateTime?)null));
                                
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
                var now = DateTime.Now;
                var nextRunTime = now.Date.AddDays(1); // Tomorrow at 00:00
                var delay = nextRunTime - now;

                // Wait until 12 AM
                await Task.Delay(delay, stoppingToken);

                await DowngradePlan();
            }
        }
    }
}