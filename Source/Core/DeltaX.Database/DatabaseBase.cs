namespace DeltaX.Database
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Data;
    using System.Threading.Tasks;

    public class DatabaseBase : IDatabaseBase
    {

        private object _locker = new object();
        private ILogger log;
        private Exception lastException;
        private string[] connectionStrings;
        private Type dbConnectionType;
        private string currentConnectionString;

        public static DatabaseBase Build<T>(string[] connectionStrings, ILogger logger = null) where T : IDbConnection, new()
        {
            return new DatabaseBase(typeof(T), connectionStrings, logger);
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

        protected DatabaseBase(Type dbConnectionType, string[] connectionStrings, ILogger logger = null)
        {
            this.dbConnectionType = dbConnectionType;
            this.connectionStrings = connectionStrings;

            if (logger != null)
            {
                log = logger;
            }
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


        /// <summary>
        /// Execute async Task Action with New DbConnection and release on finish
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method"></param>
        /// <returns>TResult</returns>
        public TResult Run<TResult>(Func<IDbConnection, Task<TResult>> method)
        {
            var t = RunAsync(method);
            t.Wait();
            return t.Result;
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
                if (!string.IsNullOrEmpty(currentConnectionString) && TryConnect(currentConnectionString))
                {
                    return true;
                }

                for (int idx = 0; idx < connectionStrings.Length; idx++)
                {
                    currentConnectionString = connectionStrings[idx];
                    if (TryConnect(currentConnectionString))
                    {
                        break;
                    }
                }
            }

            return IsConnected;
        }


        /// <summary>
        /// Get a new connection based on connectionStrings array
        /// </summary>
        /// <returns></returns>
        public IDbConnection GetConnection()
        {
            Exception exception = new ArgumentNullException("ConnectionStrings List Error");
            foreach (string connectionString in connectionStrings)
            {
                try
                {
                    var dbConn = Connect(connectionString);
                    if (dbConn.State.HasFlag(ConnectionState.Open))
                        return dbConn;

                    // Close if not openned!
                    dbConn.Close();
                }
                catch (Exception e) { exception = e; }
            }

            throw exception;
        }

        private bool TryConnect(string connectionString)
        {
            // Close and Dispose previous connection if pending
            Close();

            try
            {
                DbConnection = Connect(connectionString);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            return IsConnected;
        }


        private IDbConnection Connect(string connectionString)
        {
            try
            {
                log?.LogDebug("Database try ConnectionString: {0}", connectionString);

                IDbConnection dbConn = (IDbConnection)Activator.CreateInstance(dbConnectionType);

                dbConn.ConnectionString = connectionString;
                dbConn.Open();

                log?.LogDebug("Database State: {0}", dbConn.State);

                if (dbConn.State.HasFlag(ConnectionState.Open))
                {
                    log?.LogDebug("Database Connected ConnectionString: {0}", connectionString);
                }

                return dbConn;
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Database Connect excepcion");
                throw;
            }
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
                                OnConnect?.Invoke(this, currentConnectionString);
                            else
                                OnDisconnect?.Invoke(this, currentConnectionString);
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
                    DbConnection.Close();
                    DbConnection.Dispose();
                    DbConnection = null;
                    OnDisconnect?.Invoke(this, currentConnectionString);
                }
                prev_connected = false;
            }
            catch (Exception ex)
            {
                lastException = ex;
                log?.LogError(ex, "Database on close exception");
            }
        }
    }
}
