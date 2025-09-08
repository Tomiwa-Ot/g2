using System.Text.Json;
using G2.Infrastructure.Model;
using G2.Infrastructure.Repository;
using G2.Infrastructure.Repository.Database.Base;
using G2.Infrastructure.Repository.Database.Job;
using G2.Infrastructure.Repository.Database.User;
using G2.Infrastructure.Repository.Database.KnownHeader;
using G2.Infrastructure.Repository.MessageQueue;
using G2.Infrastructure.TechnologyDetector;
using G2.Service.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Collections.Concurrent;


namespace G2.Service.Jobs
{
    public class ConsumerService: BackgroundService, IConsumerService
    {
        private IPlaywright _playwright;
        private readonly SystemLoad _systemLoad;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly IMessageQueue _messageQueue;
        private readonly ILogger<ConsumerService> _logger;
        private const int MaxConcurrency = 10;

        public ConsumerService(SystemLoad systemLoad,
                    IServiceProvider serviceProvider,
                    IMemoryCache memoryCache,
                    IMessageQueue messageQueue,
                    ILogger<ConsumerService> logger)
        {
            _systemLoad = systemLoad;
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
            _messageQueue = messageQueue;
            _logger = logger;
            _concurrencySemaphore = new SemaphoreSlim(MaxConcurrency);
        }

        public async Task RunJob((Job job, CancellationToken stoppingToken) data)
        {
            // Track if job has been cancelled
            bool isJobCancelled = false;

            // Obtain semaphore and execute on new thread
            await _concurrencySemaphore.WaitAsync(data.stoppingToken);
            _ = Task.Run(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Load repositories
                    IJobRepository jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                    IKnownHeaderRepository knownHeaderRepository = scope.ServiceProvider.GetRequiredService<IKnownHeaderRepository>();
		            IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    IMessageQueue queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                    IWappalyzer wappalyzer = scope.ServiceProvider.GetRequiredService<IWappalyzer>();
                    _playwright = await Playwright.CreateAsync();
                    List<KnownHeader> knownHeaders = await knownHeaderRepository.FindListAsync(x 
                        => !x.IsDeleted);

                    // Find job to update
                    Job? updateJob = await jobRepository.FirstOrDefaultAsync(x =>
                        x.Id == data.job.Id && !x.IsDeleted);
                    DateTime startedAt = DateTime.UtcNow.AddHours(1);

                    try
                    {
                        // TODO progress indicator
                        // Check if memory usage is below 80%
                        if ((float) (_systemLoad.GetMemoryUsage() / _systemLoad.GetTotalMemory()) < .8f && updateJob != null)
                        {
                            ConcurrentBag<Site> _sites = [];
                            List<Technology> technologies = [];
                            List<string> ipAddresses = await DomainHelper.ResolveToIP(data.job.Url);

                            // Run wappalyzer
                            WappalyzerResult? result = await wappalyzer.Detect(data.job.Url);
                            /**if (result == null)
                            {
                                // add job back to queue
                                await queue.Enqueue(new Job()
                                {
                                    Id = updateJob.Id,
                                    Url = updateJob.Url,
                                    UserId = updateJob.UserId,
                                    Output = updateJob.Output,
                                    Screenshot = updateJob.Screenshot,
                                    Status = updateJob.Status,
                                    CreatedAt = updateJob.CreatedAt,
                                    StartedAt = updateJob.StartedAt,
                                    CompletedAt = updateJob.CompletedAt,
                                });
                                return;
                            }**/
                            technologies = [.. result.Technologies.Select(x => new Technology()
                            {
                                Name = x.Name,
                                Version = x.Version
                            })];

                            IBrowser browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
                            IBrowserContext context = await browser.NewContextAsync();
                            IPage page = await context.NewPageAsync();

                            // page.SetDefaultTimeout(60000);
                            // page.SetDefaultNavigationTimeout(60000);

                            // Check for ssrf
                            await page.RouteAsync("**/*", async route =>
                            {
                                var request = route.Request;

                                if (!await SSRFChecker.IsUrlSafe(request.Url))
                                {
                                    await route.AbortAsync();
                                    return;
                                }

                                await route.ContinueAsync();
                            });

                            page.Request += async (_, request) =>
                            {
                                if (isJobCancelled) return;

                                if (!await SSRFChecker.IsUrlSafe(request.Url)) return;

                                if (await ShouldJobBeCancelled(data.job.Id))
                                {
                                    Console.WriteLine("Job should be cancelled");
                                    await CancelJob(data.job.Id);
                                    isJobCancelled = true;
                                    return;
                                }

                                Site? site = _sites.FirstOrDefault(x => x.Url.Equals(request.Url,
                                    StringComparison.OrdinalIgnoreCase));
                                if (site == null)
                                {
                                    // Add to site list if not exist
                                    site = new Site()
                                    {
                                        Url = request.Url,
                                        RequestHeaders = [],
                                        ResponseHeaders = [],
                                        GetParams = [],
                                        PostParams = []
                                    };
                                    _sites.Add(site);

                                    // Extract unique headers
                                    site.RequestHeaders.AddRange([.. request.Headers.Where(x
                                        => !knownHeaders.Where(y => y.Type.Equals("request",
                                                StringComparison.OrdinalIgnoreCase)).ToList()
                                                .Any(z => z.Name.Equals(x.Key, StringComparison.OrdinalIgnoreCase)))
                                    .Select(x => new Infrastructure.Model.Header()
                                    {
                                        Key = x.Key,
                                        Value = x.Value
                                    })]);

                                    // Extract get parameters
                                    var queryParams = System.Web.HttpUtility.ParseQueryString(new Uri(request.Url).Query);
                                    site.GetParams.AddRange([.. queryParams.AllKeys.Select(x => new Infrastructure.Model.Get()
                                    {
                                        Key = x,
                                        Value = queryParams[x]
                                    })]);

                                    // Extract post data
                                    if (request.Method == "POST")
                                    {
                                        site.PostParams.Add(new Infrastructure.Model.Post()
                                        {
                                            Value = request.PostData
                                        });
                                    }
                                }
                            };

                            if (isJobCancelled) return;

                            page.Response += async (_, response) =>
                            {
                                if (isJobCancelled) return;
                                if (!await SSRFChecker.IsUrlSafe(response.Url)) return;

                                if (await ShouldJobBeCancelled(data.job.Id))
                                {
                                    await CancelJob(data.job.Id);
                                    isJobCancelled = true;
                                    return;
                                }

                                Site? site = _sites.FirstOrDefault(x => x.Url.Equals(response.Url,
                                    StringComparison.OrdinalIgnoreCase));
                                Console.WriteLine($"Response from {response.Url}");
                                // check perform null check
                                site.ResponseHeaders.AddRange([.. response.Headers.Where(x
                                    => !knownHeaders.Where(y => y.Type.Equals("response",
                                            StringComparison.OrdinalIgnoreCase)).ToList()
                                            .Any(z => z.Name.Equals(x.Key, StringComparison.OrdinalIgnoreCase)))
                                .Select(x => new Infrastructure.Model.Header()
                                {
                                    Key = x.Key,
                                    Value = x.Value
                                })]);
                            };

                            if (isJobCancelled) return;

                            await page.GotoAsync(data.job.Url);
                            Console.WriteLine("checking is user is allowed to screenshot");
                            Infrastructure.Model.User? account = await userRepository.FirstOrDefaultAsync(x =>
                                x.Id == updateJob.UserId && x.IsVerified && !x.IsDisabled && !x.IsDeleted,
                                includeProperties: "Plan");
                            if (account.Plan.Screenshot)
                            {
                                byte[] screenshot = await page.ScreenshotAsync(
                                new PageScreenshotOptions
                                {
                                    FullPage = true
                                });
                                updateJob.Screenshot = Convert.ToBase64String(screenshot);
                            }
                            await browser.CloseAsync();

                            updateJob.Output = JsonSerializer.Serialize(new
                            {
                                Sites = _sites,
                                Technology = technologies,
                                IPAddresses = ipAddresses
                            });
                            updateJob.Status = "Complete";
                            updateJob.StartedAt = startedAt;
                            updateJob.CompletedAt = DateTime.UtcNow.AddHours(1);

                            await unitOfWork.BeginTransactionAsync();
                            jobRepository.Update(updateJob);
                            await unitOfWork.CommitTransactionAsync();
                        }
                        else
                        {
                            // add job back to queue
                            Console.WriteLine("adding back to queue");
                            await queue.Enqueue(new Job()
                            {
                                Id = updateJob.Id,
                                Url = updateJob.Url,
                                UserId = updateJob.UserId,
                                Output = updateJob.Output,
                                Screenshot = updateJob.Screenshot,
                                Status = updateJob.Status, 
                                CreatedAt = updateJob.CreatedAt,
                                StartedAt = updateJob.StartedAt,
                                CompletedAt = updateJob.CompletedAt,
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        await unitOfWork.RollbackTransactionAsync();
                        _logger.LogError(e.Message, e);

                        // Update job as failed
                        await unitOfWork.BeginTransactionAsync();
                        updateJob.Status = "Failed";
                        updateJob.StartedAt = startedAt;
                        updateJob.CompletedAt = DateTime.UtcNow.AddHours(1);
                        jobRepository.Update(updateJob);
                        await unitOfWork.CommitTransactionAsync();
                    }
                    finally
                    {
                        _concurrencySemaphore.Release();
                    }
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _messageQueue.Dequeue(RunJob, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
            }
        }

        private async Task<bool> ShouldJobBeCancelled(long id)
        {
            return await _memoryCache.SetContains("job:cancel", id.ToString());
        }

        private async Task CancelJob(long id)
        {
            await _memoryCache.RemoveFromSet("job:cancel", id.ToString());
        }
    }
}
