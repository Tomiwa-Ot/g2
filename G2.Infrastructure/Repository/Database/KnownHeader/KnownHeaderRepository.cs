using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.KnownHeader
{
    public class KnownHeaderRepository : DbRepository<Model.KnownHeader>, IKnownHeaderRepository                     
    {
        public KnownHeaderRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}