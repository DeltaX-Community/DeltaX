namespace DeltaX.Database
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Data;

    public class DbConnectionFactory<TDbConnection> 
        where TDbConnection : IDbConnection, new()
    {
        private readonly ILogger logger;
        private readonly string[] connectionStrings;

        public DbConnectionFactory(string[] connectionStrings, ILogger logger = null)
        {
            this.connectionStrings = connectionStrings;
            this.logger = logger;
        }

        /// <summary>
        /// Get a new connection based on connectionStrings array
        /// </summary>
        /// <returns></returns>
        public TDbConnection GetConnection()
        {
            Exception exception = null;
            foreach (string conStr in connectionStrings)
            {
                try
                {
                    var dbConn = Connect(conStr);
                    if (dbConn.State.HasFlag(ConnectionState.Open))
                    {
                        return dbConn;
                    }

                    // Close if not openned!
                    dbConn.Close();
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }

            throw exception ?? new ArgumentNullException("ConnectionStrings List Error");
        }

        /// <summary>
        /// Connect to DB with specificated connectionString
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>a new DbConnection</returns>
        public TDbConnection Connect(string connectionString)
        {
            try
            {
                logger?.LogDebug("Database try ConnectionString: {0}", connectionString);

                TDbConnection dbConn = Activator.CreateInstance<TDbConnection>();

                dbConn.ConnectionString = connectionString;
                dbConn.Open();

                logger?.LogDebug("Database State: {0}", dbConn.State);

                if (dbConn.State.HasFlag(ConnectionState.Open))
                {
                    logger?.LogDebug("Database Connected ConnectionString: {0}", connectionString);
                }

                return dbConn;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Database Connect excepcion");
                throw;
            }
        }
    }
}
