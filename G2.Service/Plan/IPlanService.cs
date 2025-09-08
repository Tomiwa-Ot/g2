using G2.Infrastructure.Model;
using G2.Service.Plan.Dto.Receiving;

namespace G2.Service.Plan
{
    public interface IPlanService
    {
        Task<Response> AddPlan(AddPlanDto addPlanDto);
        Task<Response> GetAllPlans();
        Task<Response> GetPlan(long id);
        Task<Response> DeletePlan(long id);
        Task<Response> UpdatePlan(long id, UpdatePlanDto updatePlanDto);
        Task<Response> GetPlansAdmin();
    }
}