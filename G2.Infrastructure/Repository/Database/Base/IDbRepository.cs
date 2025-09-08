using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Query;

namespace G2.Infrastructure.Repository.Database.Base
{
    public interface IDbRepository<T> where T: class
    {
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<T?> GetByIdsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken);
        Task<List<T>> FindAllAsync(CancellationToken cancellationToken = default);
        Task<List<T>> FindListAsync(Expression<Func<T, bool>>? expression = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int? skip = null,
            int? take = null, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[]? includes);
        Task<List<T>> FindListWithIncludeAsync(Expression<Func<T, bool>>? expression = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, JsonSerializerOptions jsonSerializerOptions = null, CancellationToken cancellationToken = default);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, string includeProperties, CancellationToken cancellationToken = default);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default);
        Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> expression, string includeProperties, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy, CancellationToken cancellationToken = default);
        Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy, CancellationToken cancellationToken = default);
        void Update(T entity);
        Task<int> UpdateRangeAsync(Expression<Func<T, bool>> expression, Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls);
        void Delete(T entity);
    }
}