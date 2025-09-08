using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.Plan
{
    public class PlanRepository : DbRepository<Model.Plan>, IPlanRepository                     
    {
        public PlanRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}