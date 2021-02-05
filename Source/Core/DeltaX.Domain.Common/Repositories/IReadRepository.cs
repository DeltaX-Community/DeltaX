namespace DeltaX.Domain.Common.Repositories
{
    using DeltaX.Domain.Common.Entities;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IReadRepository<TEntity> : IRepository where TEntity : class, IEntity
    {
        Task<IEnumerable<TEntity>> GetListAsync(bool includeDetails = false);

        Task<long> GetCountAsync();

        Task<IEnumerable<TEntity>> GetPagedListAsync(
            int skipCount,
            int rowsPerPage,
            string filter,
            string sorting,
            object param = null,
            bool includeDetails = false);

        Task<TEntity> GetAsync(TEntity entity, bool includeDetails = false);
    }

    public interface IReadRepository<TEntity, TKey> : IReadRepository<TEntity>
        where TEntity : class, IEntity<TKey>
    {
        Task<TEntity> GetAsync(TKey id, bool includeDetails = false);
    }
}