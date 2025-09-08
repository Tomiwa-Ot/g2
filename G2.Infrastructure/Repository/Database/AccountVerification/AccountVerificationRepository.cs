using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.AccountVerification
{
    public class AccountVerificationRepository : DbRepository<Model.AccountVerification>, IAccountVerificationRepository                     
    {
        public AccountVerificationRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}