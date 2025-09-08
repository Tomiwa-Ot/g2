using Microsoft.EntityFrameworkCore;

namespace G2.Infrastructure.Repository.Database.Base
{
    public class DbFactory : IDisposable
    {
        private bool _disposed;
        private readonly Func<G2DbContext> _instanceFunc;
        private DbContext? _dbContext;
        public DbContext DBContext => _dbContext ?? (_dbContext = _instanceFunc.Invoke());


        public DbFactory(Func<G2DbContext> dbContextFactory)
        {
            _instanceFunc = dbContextFactory;
        }

        public async Task BeginTransactionAsync()
        {
            await DBContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            await DBContext.Database.CurrentTransaction?.CommitAsync()!;
        }
        public async Task RollbackTransactionAsync()
        {
            await DBContext.Database.CurrentTransaction?.RollbackAsync()!;
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbContext?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}