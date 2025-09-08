using G2.Infrastructure.Model;
using G2.Service.Plan;
using G2.Service.Plan.Dto.Receiving;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2.API.Controllers
{
    [Route("api/plan")]
    [ApiController]
    public class PlanController : ControllerBase
    {
        private readonly IPlanService _planService;

        public PlanController(IPlanService planService)
        {
            _planService = planService;
        }

        [HttpPost]
        [Authorize]
        public async Task<Response> AddPlan(AddPlanDto addPlanDto)
        {
            return await _planService.AddPlan(addPlanDto);
        }

        [HttpGet]
        public async Task<Response> GetAllPlans()
        {
            return await _planService.GetAllPlans();
        }

        [HttpGet("{id}")]
        public async Task<Response> GetPlan(long id)
        {
            return await _planService.GetPlan(id);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<Response> DeletePlan(long id)
        {
            return await _planService.DeletePlan(id);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<Response> UpdatePlan(long id, UpdatePlanDto updatePlanDto)
        {
            return await _planService.UpdatePlan(id, updatePlanDto);
        }
        
        [HttpGet("all")]
        [Authorize]
        public async Task<Response> GetPlansAdmin()
        {
            return await _planService.GetPlansAdmin();
        }
    }
}