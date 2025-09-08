using G2.Infrastructure.Model;
using G2.Service.Jobs;
using G2.Service.Jobs.Dto.Receiving;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2.API.Controllers
{
    [Route("api/job")]
    [ApiController]
    [Authorize]
    public class JobController: ControllerBase
    {
        private readonly IJobService _jobService;

        public JobController(IJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpPost]
        public async Task<Response> AddJob(AddJobDto addJobDto)
        {
            return await _jobService.AddJob(addJobDto);
        }

        [HttpGet("cancel/{id}")]
        public async Task<Response> CancelJob(long id)
        {
            return await _jobService.CancelJob(id);
        }

        [HttpGet("{id}")]
        public async Task<Response> GetJobById(long id)
        {
            return await _jobService.GetJobById(id);
        }

        [HttpGet]
        public async Task<Response> GetUserJobs([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            return await _jobService.GetUserJobs(page, limit);
        }

        [HttpGet("all")]
        public async Task<Response> GetJobs([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? query = null)
        {
            return await _jobService.GetJobs(page, limit, query);
        }

        [HttpGet("progress/{id}")]
        public async Task<Response> GetJobProgressPercentage(long id)
        {
            return await _jobService.GetJobProgressPercentage(id);
        }
    }
}