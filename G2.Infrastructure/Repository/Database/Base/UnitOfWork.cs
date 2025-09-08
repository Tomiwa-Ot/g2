namespace G2.Infrastructure.Repository.Database.Base
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbFactory _dbFactory;
        public UnitOfWork(DbFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbFactory.DBContext.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
           await _dbFactory.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _dbFactory.DBContext.SaveChangesAsync();
                await _dbFactory.CommitTransactionAsync();
            }
            catch
            {
                await _dbFactory.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_dbFactory.DBContext.Database.CurrentTransaction != null)
            {
                await _dbFactory.RollbackTransactionAsync();
            }
        }

    }
}