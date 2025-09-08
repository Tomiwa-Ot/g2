using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.Job
{
    public class JobRepository : DbRepository<Model.Job>, IJobRepository                     
    {
        public JobRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}