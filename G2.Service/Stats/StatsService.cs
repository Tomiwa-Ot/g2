using G2.Infrastructure.Model;
using G2.Infrastructure.Repository.Database.Job;
using G2.Infrastructure.Repository.Database.Transaction;
using G2.Infrastructure.Repository.Database.User;
using G2.Service.Helper;
using Microsoft.Extensions.Logging;

namespace G2.Service.Stats
{
    public class StatsService : IStatsService
    {
        private readonly ProfileHelper _profileHelper;
        private readonly SystemLoad _systemLoad;
        private readonly IJobRepository _jobRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<StatsService> _logger;
        public StatsService(ProfileHelper profileHelper,
            SystemLoad systemLoad,
            IJobRepository jobRepository,
            ITransactionRepository transactionRepository,
            IUserRepository userRepository,
            ILogger<StatsService> logger)
        {
            _profileHelper = profileHelper;
            _systemLoad = systemLoad;
            _jobRepository = jobRepository;
            _transactionRepository = transactionRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Response> Health()
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null 
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                return ResponseBuilder.Send(ResponseStatus.success, "", new
                {
                    MemoryUsed = _systemLoad.GetMemoryUsage(),
                    MemoryTotal = _systemLoad.GetTotalMemory(),
                    StorageUsed = _systemLoad.GetMemoryUsage(),
                    StorageTotal = _systemLoad.GetTotalStorage()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> Jobs(DateTime? from, DateTime? to)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                List<Job> jobs = [];
                if (from.HasValue && to.HasValue)
                {
                    jobs = await _jobRepository.FindListAsync(x =>
                        x.CreatedAt >= from.Value && x.CreatedAt <= to.Value);
                }
                else
                {
                    DateTime fromDate = DateTime.UtcNow.Date.AddDays(-7);
                    DateTime toDate = DateTime.UtcNow.Date;
                    jobs = await _jobRepository.FindListAsync(x =>
                            x.CreatedAt >= fromDate && x.CreatedAt <= toDate);
                }

                var summary = jobs.GroupBy(x => x.CreatedAt.Date)
                    .Select(y => new
                    {
                        Date = y.Key,
                        Amount = y.Count()
                    });
                return ResponseBuilder.Send(ResponseStatus.success, "", summary);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> Payment(DateTime? from, DateTime? to)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                List<Infrastructure.Model.Transaction> transactions = [];
                if (from.HasValue && to.HasValue)
                {
                    transactions = await _transactionRepository.FindListAsync(x =>
                        x.CreatedAt >= from.Value && x.CreatedAt <= to.Value);
                }
                else
                {
                    DateTime fromDate = DateTime.UtcNow.Date.AddDays(-7);
                    DateTime toDate = DateTime.UtcNow.Date;
                    transactions = await _transactionRepository.FindListAsync(x =>
                            x.CreatedAt >= fromDate && x.CreatedAt <= toDate);
                }

                var summary = transactions.GroupBy(x => x.CreatedAt.Date)
                    .Select(y => new
                    {
                        Date = y.Key,
                        Amount = y.Count()
                    });
                return ResponseBuilder.Send(ResponseStatus.success, "", summary);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> Users(DateTime? from, DateTime? to)
        {
            try
            {
                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null
                    || !user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                List<Infrastructure.Model.User> users = [];
                if (from.HasValue && to.HasValue)
                {
                    users = await _userRepository.FindListAsync(x =>
                        x.CreatedAt >= from.Value && x.CreatedAt <= to.Value);
                }
                else
                {
                    DateTime fromDate = DateTime.UtcNow.Date.AddDays(-7);
                    DateTime toDate = DateTime.UtcNow.Date;
                    users = await _userRepository.FindListAsync(x =>
                            x.CreatedAt >= fromDate && x.CreatedAt <= toDate);
                }

                var summary = users.GroupBy(x => x.CreatedAt.Date)
                    .Select(y => new
                    {
                        Date = y.Key,
                        Amount = y.Count()
                    });
                return ResponseBuilder.Send(ResponseStatus.success, "", summary);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }
    }
}