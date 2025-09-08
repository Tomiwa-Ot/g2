using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace G2.Infrastructure.Repository.Database.Base
{
    public class DbRepository<T> : IDbRepository<T> where T : class
    {
        private readonly DbFactory _dbFactory;
        private DbSet<T>? _dbSet;

        protected DbSet<T> DbSet
        {
            get => _dbSet ?? (_dbSet = _dbFactory.DBContext.Set<T>());
        }

        public DbRepository(DbFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<T> AddAsync(T entity)
        {
             var data = await DbSet.AddAsync(entity);

            return data.Entity;
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await DbSet.AddRangeAsync(entities);
        }

        public void Delete(T entity)
        {
            DbSet.Entry(entity).State = EntityState.Modified;
        }

        public async Task<List<T>> FindAllAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet.ToListAsync(cancellationToken);
        }

            public async Task<List<T>> FindListAsync(Expression<Func<T, bool>>? expression = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int? skip = null,
                int? take = null, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[]? includes)
            {
                var query = expression != null ? DbSet.Where(expression) : DbSet;
                
                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                if (orderBy != null)
                    query = orderBy(query);

                if (skip.HasValue)
                    query = query.Skip(skip.Value);

                if (take.HasValue)
                    query = query.Take(take.Value);

                return await query.ToListAsync(cancellationToken);
            }

        public async Task<List<T>> FindListWithIncludeAsync(Expression<Func<T, bool>>? expression = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, JsonSerializerOptions jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = DbSet;

            if (include != null)
            {
                query = include(query);
            }

            if (expression != null)
            {
                query = query.Where(expression);
            }

            if (jsonSerializerOptions != null)
            {
                var options = new JsonSerializerOptions(jsonSerializerOptions);
                return await query.ToListAsync(cancellationToken);
            }
           
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.ToListAsync(cancellationToken);

        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
        {
            var query = DbSet.AsQueryable();

            return await query.FirstOrDefaultAsync(expression, cancellationToken);
        }

        public async Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await DbSet.FindAsync(id,cancellationToken);
        }

        public async Task<T?> GetByIdsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate), "Predicate cannot be null.");
            }

            return await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public void Update(T entity)
        {
            DbSet.Entry(entity).State = EntityState.Modified;
        }

        public async Task<int> UpdateRangeAsync(Expression<Func<T, bool>> expression, Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls)
        {
            return await DbSet.AsQueryable()
                .Where(expression)
                .ExecuteUpdateAsync(setPropertyCalls);
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, string includeProperties, CancellationToken cancellationToken = default)
        {
            var query = DbSet.AsQueryable();


            query = includeProperties.Split(new char[] { ',' },
                StringSplitOptions.RemoveEmptyEntries).Aggregate(query, (current, includeProperty)
                => current.Include(includeProperty));

            return await query.FirstOrDefaultAsync(expression, cancellationToken);
        }

        public async Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> expression, string includeProperties, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            var query = DbSet.AsQueryable();


            query = includeProperties.Split(new char[] { ',' },
                StringSplitOptions.RemoveEmptyEntries).Aggregate(query, (current, includeProperty)
                => current.Include(includeProperty));

            if (orderBy != null)
                query = orderBy(query);
        
            return await query.LastOrDefaultAsync(expression, cancellationToken);
        }

        public async Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            var query = DbSet.AsQueryable();
            if (orderBy != null)
                query = orderBy(query);

            return await query.LastOrDefaultAsync(expression, cancellationToken);
        }
    }
}