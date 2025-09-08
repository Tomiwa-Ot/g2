using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.User
{
    public class UserRepository : DbRepository<Model.User>, IUserRepository                     
    {
        public UserRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}