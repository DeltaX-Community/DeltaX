﻿namespace DeltaX.Database
{
    using Microsoft.Extensions.Logging;
    using System; 
    using System.Data; 
    using System.Threading.Tasks;

    public class Database<T> : DatabaseBase, IDatabaseBase 
        where T : IDbConnection, new()
    {
        public Database(string[] connectionStrings, ILoggerFactory loggerFactory = null)
            : base(typeof(T), connectionStrings, loggerFactory) { }

        public new T DbConnection => (T)base.DbConnection;

        public new T GetConnection()
        {
            return (T)base.GetConnection();
        }

        public TResult RunSync<TResult>(Func<T, TResult> method)
        {
            return base.RunSync((dbConn) => method((T)dbConn));
        }

        public Task<TResult> RunAsync<TResult>(Func<T, Task<TResult>> method)
        {
            return base.RunAsync((dbConn) => method((T)dbConn));
        }

        public TResult Run<TResult>(Func<T, Task<TResult>> method)
        {
            return base.Run((dbConn) => method((T)dbConn));
        }

        public TResult Run<TResult>(Func<T, TResult> method)
        {
            return base.Run((dbConn) => method((T)dbConn));
        }

        public TResult RunTransaction<TResult>(Func<T, IDbTransaction, TResult> method)
        {
            return base.Run((dbConn) => {
                using (var transaction = dbConn.BeginTransaction())
                {
                    var result = method((T)dbConn, transaction);
                    transaction.Commit();
                    return result;
                }
            });
        }
    }
}
