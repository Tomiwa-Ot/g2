using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.Transaction
{
    public interface ITransactionRepository : IDbRepository<Model.Transaction>                        
    {
    }
}