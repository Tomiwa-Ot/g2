using AutoMapper;
using FluentValidation.Results;
using G2.Infrastructure.Model;
using G2.Infrastructure.Repository;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.Job;
using G2.Infrastructure.Repository.Database.Plan;
using G2.Infrastructure.Repository.Database.User;
using G2.Infrastructure.Repository.MessageQueue;
using G2.Service.Helper;
using G2.Service.Jobs.Dto.Receiving;
using G2.Service.Jobs.Dto.Transfer;
using G2.Service.Jobs.Validation;
using Microsoft.Extensions.Logging;

namespace G2.Service.Jobs
{
    public class JobService : IJobService
    {
        private readonly AddJobValidator _addJobValidator;
        private readonly IJobRepository _jobRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly IMessageQueue _messageQueue;
        private readonly ProfileHelper _profileHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<JobService> _logger;
        
        public JobService(AddJobValidator addJobValidator,
                        IJobRepository jobRepository,
                        IPlanRepository planRepository,
                        IUserRepository userRepository,
                        IMemoryCache memoryCache,
                        IMessageQueue messageQueue,
                        ProfileHelper profileHelper,
                        IUnitOfWork unitOfWork,
                        IMapper mapper,
                        ILogger<JobService> logger)
        {
            _addJobValidator = addJobValidator;
            _jobRepository = jobRepository;
            _planRepository = planRepository;
            _userRepository = userRepository;
            _memoryCache = memoryCache;
            _messageQueue = messageQueue;
            _profileHelper = profileHelper;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<Response> AddJob(AddJobDto addJobDto)
        {
            try
            {
                // Validate input
                ValidationResult validationResult = await _addJobValidator.ValidateAsync(addJobDto);
                if (!validationResult.IsValid)
                {
                    return ResponseBuilder.Send(ResponseStatus.invalid_format, "Data not formatted properly", validationResult.Errors);
                }

                // Get user details
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                await _unitOfWork.BeginTransactionAsync();

                Infrastructure.Model.User? account = await _userRepository.FirstOrDefaultAsync(x
                    => x.Id == user.Id, includeProperties: "Plan");
                if (account == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                // Check if paid plan has expired
                if (account.PlanExpiration.HasValue && DateTime.Today > account.PlanExpiration)
                {
                    Infrastructure.Model.Plan? freeplan = await _planRepository.FirstOrDefaultAsync(
                        x => x.Name.Equals("free", StringComparison.OrdinalIgnoreCase));

                    account.PlanId = freeplan.Id == null ? 1 : freeplan.Id;
                    account.PlanExpiration = null;
                    _userRepository.Update(account);
                    await _unitOfWork.SaveChangesAsync();
                }

                account = await _userRepository.FirstOrDefaultAsync(x
                    => x.Id == user.Id, includeProperties: "Plan");

                // Check if user has exceeded daily quota
                List<Job> jobs = await _jobRepository.FindListWithIncludeAsync(x =>
                    x.CreatedAt.Date == DateTime.Today && x.User.Id == account.Id && !x.IsDeleted);
                if (jobs.Count >= account.Plan.Quota + account.ReferralQuota)
                {
                    return ResponseBuilder.Send(ResponseStatus.quota_exceeded, 
                        "Daily quota has been exceeded. Upgrade plan or try again tomorrow", null);
                }

                // Check number of concurrent jobs
                List<Job> concurrentJobs = [.. jobs.Where(x => x.CompletedAt == null)];
                if (concurrentJobs.Count >= account.Plan.Concurrency)
                {
                    return ResponseBuilder.Send(ResponseStatus.max_concurrent_jobs, 
                        $"{account.Plan.Name} only supports {account.Plan.Quota} concurrent jobs", null);
                }

                // Create job
                Job newJob = _mapper.Map<Job>(addJobDto);
                newJob.CreatedAt = DateTime.UtcNow.AddHours(1);
                newJob.IsDeleted = false;
                newJob.UserId = (long)user.Id;
                newJob.Status = "Waiting";

                Job createdJob = await _jobRepository.AddAsync(newJob);
                await _unitOfWork.CommitTransactionAsync();
                await _messageQueue.Enqueue(new Job()
                {
                    Id = createdJob.Id,
                    Url = createdJob.Url,
                    UserId = createdJob.UserId,
                    Output = createdJob.Output,
                    Screenshot = createdJob.Screenshot,
                    Status = createdJob.Status, 
                    CreatedAt = createdJob.CreatedAt,
                    StartedAt = createdJob.StartedAt,
                    CompletedAt = createdJob.CompletedAt,
                });

                return ResponseBuilder.Send(ResponseStatus.success, "Success", new { createdJob.Id });
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> CancelJob(long id)
        {
            try
            {
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                Job? job = await _jobRepository.FirstOrDefaultAsync(x => x.Id == id 
                    && x.UserId == user.Id && !x.IsDeleted);
                if (job == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
                }

                await _memoryCache.AddToSet("job:cancel", id.ToString());
                await _unitOfWork.BeginTransactionAsync();

                job.Status = "Cancelled";
                _jobRepository.Update(job);

                await _unitOfWork.CommitTransactionAsync();

                return ResponseBuilder.Send(ResponseStatus.success, "Job cancelled", null);
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetJobById(long id)
        {
            try
            {
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                Job? job = await _jobRepository.FirstOrDefaultAsync(x => x.Id == id
                    && (user.Role.Contains("admin", StringComparison.OrdinalIgnoreCase) || x.UserId == user.Id)
                    && !x.IsDeleted);
                if (job == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Job not found", null);
                }

                JobDto jobDto = _mapper.Map<JobDto>(job);
                return ResponseBuilder.Send(ResponseStatus.success, "Success", jobDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetJobProgressPercentage(long id)
        {
            try
            {
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                Job? job = await _jobRepository.FirstOrDefaultAsync(x => x.Id == id
                    && x.UserId == user.Id && !x.IsDeleted);

                if (job == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.failure, "Job not found", null);
                }

                if (job.CompletedAt != null)
                {
                    return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                    {
                        Progress = 100
                    });
                }

                if (job.Status == "Waiting" || job.Status == "Cancelled")
                {
                    return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                    {
                        Progress = 0.00
                    });
                }

                var progress = await _memoryCache.GetString($"job:status:{id}");
                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    Progress = progress
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetUserJobs(int page = 1, int limit = 10)
        {
            try
            {
                var user = _profileHelper.GetUserDetails();
                if (user.Id == null || user.Email == null || user.Role == null)
                {
                    return ResponseBuilder.Send(ResponseStatus.unauthorised, "Unauthorised", null);
                }

                page = Math.Max(page, 1);
                limit = Math.Clamp(limit, 1, 100);
                int skip = (page - 1) * limit;

                // Find total results
                List<Job> jobList = await _jobRepository.FindListAsync(x
                    => x.UserId == user.Id && !x.IsDeleted);

                // paginate
                List<Job> jobs = await _jobRepository.FindListAsync(x
                    => x.UserId == user.Id && !x.IsDeleted,
                    orderBy: q => q.OrderByDescending(e => e.Id),
                    skip: skip,
                    take: limit);
                //List<JobDto> jobsDto = _mapper.Map<List<JobDto>>(jobs);

                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    TotalSize = jobList.Count,
                    Items = jobs.Select(x => new {
                    	x.Id,
                    	x.Url,
                    	x.Status,
                    	x.CreatedAt,
                    	x.StartedAt,
                    	x.CompletedAt
                    }).ToList()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }

        public async Task<Response> GetJobs(int page = 1, int limit = 10, string? query = null)
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

                page = Math.Max(page, 1);
                limit = Math.Clamp(limit, 1, 100);
                int skip = (page - 1) * limit;

                // get total size
                List<Job> jobsList = await _jobRepository.FindListAsync(x =>
                    !x.IsDeleted);

                List<Job> jobs = await _jobRepository.FindListAsync(x =>
                    !x.IsDeleted &&
                    (string.IsNullOrEmpty(query)
                        || x.User.Fullname.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || x.User.Email.Contains(query, StringComparison.OrdinalIgnoreCase)),
                    orderBy: q => q.OrderByDescending(r => r.Id),
                    skip: skip,
                    take: limit,
                    includes: x => x.User);

                var response = jobs.Select(x => new
                {
                    x.Id,
                    x.User.Email,
                    x.User.Fullname,
                    x.Url,
                    x.Status,
                    x.CreatedAt
                }).ToList();
                return ResponseBuilder.Send(ResponseStatus.success, "Success", new
                {
                    TotalSize = jobsList.Count,
                    Items = response
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                return ResponseBuilder.Send(ResponseStatus.failure, "Failed", null);
            }
        }
    }
}
