using G2.Infrastructure.Model;
using G2.Service.Jobs.Dto.Receiving;

namespace G2.Service.Jobs
{
    public interface IJobService
    {
        Task<Response> AddJob(AddJobDto addJobDto);
        Task<Response> CancelJob(long id);
        Task<Response> GetJobById(long id);
        Task<Response> GetUserJobs(int page = 1, int limit = 10);
        Task<Response> GetJobProgressPercentage(long id);
        Task<Response> GetJobs(int page = 1, int limit = 10, string? query = null);
        // Task UpdateJob(string? status = null, 
        //     DateTime? startedAt = null, DateTime? completedAt = null);
    }
}