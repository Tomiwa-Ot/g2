using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.PromoCode
{
    public class PromoCodeRepository : DbRepository<Model.PromoCode>, IPromoCodeRepository                     
    {
        public PromoCodeRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}