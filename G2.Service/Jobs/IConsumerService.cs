using G2.Infrastructure.Model;

namespace G2.Service.Jobs
{
    public interface IConsumerService
    {
        Task RunJob((Job job, CancellationToken stoppingToken) data);
    }
}