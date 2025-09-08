using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.Transaction
{
    public class TransactionRepository : DbRepository<Model.Transaction>, ITransactionRepository                     
    {
        public TransactionRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}