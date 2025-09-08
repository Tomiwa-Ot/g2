using G2.Infrastructure.Model;
using G2.Service.Stats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2.API.Controllers
{
    [Route("api/stats")]
    [ApiController]
    [Authorize]
    public class StatsController : ControllerBase
    {
        private readonly IStatsService _statsService;

        public StatsController(IStatsService statsService)
        {
            _statsService = statsService;
        }

        [HttpGet("health")]
        public async Task<Response> Health()
        {
            return await _statsService.Health();
        }

        [HttpGet("user")]
        public async Task<Response> Users([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return await _statsService.Users(from, to);
        }

        // [HttpGet("user/{id}")]
        // public async Task<Response> UserSummary(long id)
        // {
        //     return await _statsService.UserSummary(id);
        // }

        [HttpGet("job")]
        public async Task<Response> Jobs([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return await _statsService.Jobs(from, to);
        }

        [HttpGet("payment")]
        public async Task<Response> Payment([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return await _statsService.Payment(from, to);
        }
    }
}