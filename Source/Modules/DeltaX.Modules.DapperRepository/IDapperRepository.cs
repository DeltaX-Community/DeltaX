using DeltaX.Domain.Common.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DeltaX.Modules.DapperRepository
{
    public interface IDapperRepository: IRepository
    {
        IDbConnection Conn { get; }
        IDbTransaction Tran { get; }  

        Task DeleteAsync<TEntity>(string whereClause, object param) where TEntity : class;
        Task DeleteAsync<TEntity>(TEntity entity) where TEntity : class;
        TEntity Get<TEntity>(object param) where TEntity : class;
        TEntity Get<TEntity>(string whereClause, object param) where TEntity : class;
        TEntity Get<TEntity>(TEntity entity) where TEntity : class;
        long GetCount<TEntity>(string whereClause, object param) where TEntity : class;
        long GetCount<TEntity>(TEntity entity) where TEntity : class;
        IEnumerable<TEntity> GetItems<TEntity>(Expression<Func<TEntity, bool>> propertiesCondition) where TEntity : class;
        IEnumerable<TEntity> GetPagedList<TEntity>(int skipCount = 0, int rowsPerPage = 1000, string whereClause = null, string orderByClause = null, object param = null) where TEntity : class;
        Task<Tkey> InsertAsync<TEntity, Tkey>(TEntity item, IEnumerable<string> fieldsToInsert = null) where TEntity : class;
        Task<TEntity> InsertAsync<TEntity>(TEntity item, IEnumerable<string> fieldsToInsert = null) where TEntity : class;
        Task<int> UpdateAsync<TEntity>(string whereClause, object param, IEnumerable<string> fieldsToSet = null) where TEntity : class;
        Task<int> UpdateAsync<TEntity>(TEntity entity, IEnumerable<string> fieldsToSet = null) where TEntity : class;
    }
}