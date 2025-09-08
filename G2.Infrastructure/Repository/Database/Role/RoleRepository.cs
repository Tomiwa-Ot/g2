using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.Role
{
    public class RoleRepository : DbRepository<Model.Role>, IRoleRepository                     
    {
        public RoleRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}