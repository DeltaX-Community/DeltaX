namespace DeltaX.Domain.Common.Repositories
{
    using DeltaX.Domain.Common.Entities;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IBasicRepository<TEntity> : IReadRepository<TEntity>
        where TEntity : class, IEntity
    {
        Task<TEntity> InsertAsync(TEntity entity);

        Task<TEntity> UpdateAsync(TEntity entity);

        Task DeleteAsync(TEntity entity); 
    }

    public interface IBasicRepository<TEntity, TKey> : IBasicRepository<TEntity>, IReadRepository<TEntity, TKey>
        where TEntity : class, IEntity, IEntity<TKey>
    {
        // Remove this because change tracker
        // Task DeleteAsync(TKey id);
    }
}
