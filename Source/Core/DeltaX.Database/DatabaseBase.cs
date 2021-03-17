namespace DeltaX.Database
{
    using Microsoft.Extensions.Logging;
    using DeltaX.Configuration;
    using System;
    using System.Data;
    using System.Threading.Tasks;


    public class DatabaseBase : IDatabaseBase
    {

        private object _locker = new object();
        private ILogger logger;
        private DbConnectionFactory dbConnFactory;
        private Exception lastException; 

        public static DatabaseBase Build<T>(string[] connectionStrings, ILoggerFactory loggerFactory = null) 
            where T : IDbConnection, new()
        {
            return new DatabaseBase(typeof(T), connectionStrings, loggerFactory);
        }

        public static DatabaseBase Build(Type dbConnectionType, string[] connectionStrings, ILoggerFactory loggerFactory = null) 
        {
            return new DatabaseBase(dbConnectionType, connectionStrings, loggerFactory);
        }

        /// <summary>
        /// Current DbConnection.
        /// 
        /// Reconnect whit TryReconnect
        /// </summary>
        public IDbConnection DbConnection { get; private set; }


        /// <summary>
        /// Event on disconnect with CurrentConnectionString as argument
        /// </summary>
        public event EventHandler<string> OnDisconnect;

        /// <summary>
        /// Event on connect with CurrentConnectionString as argument
        /// </summary>
        public event EventHandler<string> OnConnect;

        protected DatabaseBase(Type dbConnectionType, string[] connectionStrings, ILoggerFactory loggerFactory = null)
        {
            loggerFactory ??= Configuration.DefaultLoggerFactory;
            this.logger = loggerFactory.CreateLogger($"{nameof(DatabaseBase)}");

            this.dbConnFactory = new DbConnectionFactory(dbConnectionType, connectionStrings, logger); 
        }


        /// <summary>
        /// Execute Action with current DbConnection if valid 
        /// else throw last connection exception
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method"></param>
        /// <returns></returns>
        public TResult RunSync<TResult>(Func<IDbConnection, TResult> method)
        {
            lock (_locker)
            {
                if (TryReconnect())
                {
                    return method.Invoke(DbConnection);
                }
                else
                {
                    throw lastException;
                }
            }
        }

        /// <summary>
        /// Execute async Task Action with New DbConnection and release on finish
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method"></param>
        /// <returns>async Task<TResult> </returns>
        public async Task<TResult> RunAsync<TResult>(Func<IDbConnection, Task<TResult>> method)
        {
            using (var dbConn = GetConnection())
            {
                return await method.Invoke(dbConn);
            }
        }

        public async Task<TResult> RunTransactionAsync<TResult>(Func<IDbConnection, IDbTransaction, Task<TResult>> method)
        {
            using (var dbConn = GetConnection())
            {  
                using (var transaction = dbConn.BeginTransaction())
                {
                    var result = await method(dbConn, transaction);
                    transaction.Commit();
                    return result;
                }
            }
        }

        /// <summary>
        /// Execute Action with New DbConnection and release on finish
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method"></param>
        /// <returns>TResult</returns>
        public TResult Run<TResult>(Func<IDbConnection, TResult> method)
        {
            using (var dbConn = GetConnection())
            {
                return method.Invoke(dbConn);
            }
        }

        public TResult RunTransaction<TResult>(Func<IDbConnection, IDbTransaction, TResult> method)
        {
            using (var dbConn = GetConnection())
            {
                using (var transaction = dbConn.BeginTransaction())
                {
                    var result = method(dbConn, transaction);
                    transaction.Commit();
                    return result;
                }
            } 
        }  

        /// <summary>
        /// Re-Connect to db
        /// based on last valid connectionString and connectionStrings array
        /// </summary>
        /// <returns>true if connected</returns>
        public bool TryReconnect()
        {
            if (IsConnected)
            {
                return true;
            }

            lock (_locker)
            {
                // Try connect with CurrentConnectionString
                if (!string.IsNullOrEmpty(DbConnection?.ConnectionString))
                {
                    try
                    {
                        DbConnection = dbConnFactory.Connect(DbConnection.ConnectionString);
                        return true;
                    }
                    catch { }                      
                }

                try
                {
                    DbConnection = dbConnFactory.GetConnection(); 
                }
                catch { }
            }

            return IsConnected;
        }


        /// <summary>
        /// Get a new connection based on connectionStrings array
        /// </summary>
        /// <returns></returns>
        public IDbConnection GetConnection()
        {
            return dbConnFactory.GetConnection();
        } 


        bool prev_connected = false;
        /// <summary>
        /// Indica si la base de datos esta conectada
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (_locker)
                {
                    var connected = DbConnection != null && DbConnection.State.HasFlag(ConnectionState.Open);
                    if (prev_connected != connected)
                    {
                        prev_connected = connected;
                        try
                        {
                            if (connected)
                                OnConnect?.Invoke(this, DbConnection.ConnectionString);
                            else
                                OnDisconnect?.Invoke(this, DbConnection.ConnectionString);
                        }
                        catch { }
                    }

                    return connected;
                }
            }
        }


        /// <summary>
        /// Close and Dispose current DbConnection
        /// </summary>
        public void Close()
        {
            try
            {
                if (DbConnection != null)
                {
                    OnDisconnect?.Invoke(this, DbConnection.ConnectionString);
                    DbConnection.Close();
                    DbConnection.Dispose();
                    DbConnection = null;                    
                }
                prev_connected = false;
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger?.LogError(ex, "Database on close exception");
            }
        }
    }
}
