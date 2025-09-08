using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.Referral
{
    public class ReferralRepository : DbRepository<Model.Referral>, IReferralRepository                     
    {
        public ReferralRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}