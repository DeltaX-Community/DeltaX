namespace DeltaX.Modules.DapperRepository
{
    using Dapper;
    using DeltaX.LinSql.Query;
    using DeltaX.LinSql.Table;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class DapperRepositoryBase : IDapperRepository
    {
        protected TableQueryFactory queryFactory;
        protected ILogger logger;

        public DapperRepositoryBase(IUnitOfWork uow, TableQueryFactory queryFactory, ILogger logger = null)
        {
            this.UnitOfWork = uow;
            this.queryFactory = queryFactory;
            this.logger = logger;
        }

        public IUnitOfWork UnitOfWork { get; private set; }
        public IDbConnection Conn => UnitOfWork.Connection;
        public IDbTransaction Tran => UnitOfWork.Transaction;


        public Task DeleteAsync<TEntity>(TEntity entity)
            where TEntity : class
        {
            var query = queryFactory.GetDeleteQuery<TEntity>();
            logger.LogDebug("DeleteAsync query:{query} entity:{@entity}", query, entity);
            return Conn.ExecuteAsync(query, entity, Tran);
        }

        public Task DeleteAsync<TEntity>(string whereClause, object param)
            where TEntity : class
        {
            var query = queryFactory.GetDeleteQuery<TEntity>(whereClause);
            logger.LogDebug("DeleteAsync query:{query} whereClause:{whereClause} param:{@param}", query, whereClause, param);
            return Conn.ExecuteAsync(query, param, Tran);
        }

        public async Task<TEntity> InsertAsync<TEntity>(TEntity item, IEnumerable<string> fieldsToInsert = null)
            where TEntity : class
        {
            var query = queryFactory.GetInsertQuery<TEntity>(fieldsToInsert);

            var identityColumn = queryFactory.GetTable<TEntity>().GetIdentityColumn();
            if (identityColumn != null)
            {
                query += "; " + queryFactory.DialectQuery.IdentityQueryFormatSql;
                logger.LogDebug("InsertAsync query:{query} item:{@item}", query, item);
                var fieldId = await Conn.ExecuteScalarAsync(query, item, Tran);

                // Set Property Value  
                var propertyColumn = identityColumn.GetPropertyInfo();
                propertyColumn.SetValue(item, Convert.ChangeType(fieldId, propertyColumn.PropertyType));
            }
            else
            {
                logger.LogDebug("InsertAsync query:{query} item:{@item}", query, item);
                await Conn.ExecuteAsync(query, item, Tran);
            }

            return item;
        }

        public Task<Tkey> InsertAsync<TEntity, Tkey>(TEntity item, IEnumerable<string> fieldsToInsert = null)
            where TEntity : class
        {
            var query = queryFactory.GetInsertQuery<TEntity>(fieldsToInsert);
            query += "; " + queryFactory.DialectQuery.IdentityQueryFormatSql;
            logger.LogDebug("InsertAsync query:{query} item:{@item}", query, item);

            return Conn.ExecuteScalarAsync<Tkey>(query, item, Tran);
        }

        public TEntity Get<TEntity>(object param)
            where TEntity : class
        {
            var query = queryFactory.GetSingleQuery<TEntity>();
            // logger.LogDebug("GetAsync query:{query} param:{@param}", query, param);

            return Conn.QueryFirstOrDefault<TEntity>(query, param, Tran);
        }

        public TEntity Get<TEntity>(TEntity entity)
            where TEntity : class
        {
            return Get<TEntity>((object)entity);
        }

        public TEntity Get<TEntity>(string whereClause, object param)
            where TEntity : class
        {
            var query = queryFactory.GetSingleQuery<TEntity>(whereClause);
            // logger.LogDebug("GetAsync query:{query} whereClause:{whereClause} param:{@param}", query, whereClause, param);

            return Conn.QueryFirstOrDefault<TEntity>(query, param, Tran);
        }

        public IEnumerable<TEntity> GetPagedList<TEntity>(
            int skipCount = 0,
            int rowsPerPage = 1000,
            string whereClause = null,
            string orderByClause = null,
            object param = null)
            where TEntity : class
        {
            if (!string.IsNullOrEmpty(whereClause))
            {
                if (!whereClause.TrimStart().StartsWith("WHERE", true, null))
                {
                    whereClause = "WHERE " + whereClause.Trim();
                }
            }
            if (!string.IsNullOrEmpty(orderByClause))
            {
                if (!orderByClause.TrimStart().StartsWith("ORDER BY", true, null))
                {
                    orderByClause = "ORDER BY " + orderByClause.Trim();
                }
            }

            var query = queryFactory.GetPagedListQuery<TEntity>(skipCount, rowsPerPage, whereClause, orderByClause);
            // logger.LogDebug("GetPagedListAsync query:{query} whereClause:{whereClause} param:{@param}", query, whereClause, param);

            return Conn.Query<TEntity>(query, param, Tran);
        }

        public IEnumerable<TEntity> GetItems<TEntity>(
            Expression<Func<TEntity, bool>> propertiesCondition)
            where TEntity : class
        {
            var query = new QueryBuilder<TEntity>()
               .Where(propertiesCondition)
               .SelectAll()
               .GetSqlParameters();

            // logger.LogDebug("GetItemsAsync query:{query} param:{@param}", query.sql, query.parameters);

            return Conn.Query<TEntity>(query.sql, query.parameters, Tran);
        }

        public Task<int> UpdateAsync<TEntity>(
            string whereClause,
            object param,
            IEnumerable<string> fieldsToSet = null)
            where TEntity : class
        {
            var query = queryFactory.GetUpdateQuery<TEntity>(whereClause, fieldsToSet);
            logger.LogDebug("UpdateAsync query:{query} whereClause:{whereClause} param:{@param}", query, whereClause, param);

            return Conn.ExecuteAsync(query, param, Tran);
        }

        public Task<int> UpdateAsync<TEntity>(
            TEntity entity,
            IEnumerable<string> fieldsToSet = null)
            where TEntity : class
        {
            var query = queryFactory.GetUpdateQuery<TEntity>(null, fieldsToSet);
            logger.LogDebug("UpdateAsync query:{query} entity:{@entity}", query, entity);

            return Conn.ExecuteAsync(query, entity, Tran);
        }

        public long GetCount<TEntity>(TEntity entity)
            where TEntity : class
        {
            var query = queryFactory.GetCountQuery<TEntity>();
            // logger.LogDebug("GetCountAsync query:{query} entity:{@entity}", query, entity);

            return Conn.ExecuteScalar<long>(query, entity, Tran);
        }

        public long GetCount<TEntity>(string whereClause, object param)
            where TEntity : class
        {
            var query = queryFactory.GetCountQuery<TEntity>(whereClause);
            // logger.LogDebug("GetCountAsync query:{query} whereClause:{whereClause} param:{@param}", query, whereClause, param);

            return Conn.ExecuteScalar<long>(query, param, Tran);
        }
    }
}
