using DeltaX.ActivatorFactory;
using DeltaX.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;



public class DatabaseManager : IDatabaseManager
{
    private ILogger logger;
    private IConfiguration configuration;
    private ConcurrentDictionary<string, DbConnectionFactory> databases;
    private static readonly Regex regParameters = new Regex(@"@\w+", RegexOptions.Compiled);

    public DatabaseManager(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger(nameof(DatabaseManager));
        this.configuration = configuration;
        databases = new ConcurrentDictionary<string, DbConnectionFactory>();
    }

    private IDbConnection LoadDatabase(string connectionFactory)
    {
        DbConnectionFactory factory;
        if (!databases.TryGetValue(connectionFactory, out factory))
        {
            databases[connectionFactory] = GetDbConnectionFactory(connectionFactory);
        }

        return databases[connectionFactory].GetConnection();
    }


    public DbConnectionFactory GetDbConnectionFactory(string databaseConnectionSectionName = "DefaultDatabaseConnection")
    {
        var connectionSection = new DatabaseConnectionSection();
        configuration.GetSection(databaseConnectionSectionName).Bind(connectionSection);

        string[] assembyAndClass = connectionSection.Type.Split(";");
        var type = InstanceCreator.GetType(assembyAndClass[0].Trim(), assembyAndClass[1].Trim());

        return new DbConnectionFactory(type, new[] { connectionSection.ConnectionString }, logger);
    }

    private object ValueOrDbNull(object value)
    {
        if(value is double val)
        {
            if (double.IsNaN(val) || double.IsInfinity(val))
                return DBNull.Value;
        }

        return value ?? DBNull.Value;
    }     

    public int SaveToDb(string connectionFactory, string commandText, CommandType commandType, Dictionary<string, object> parameters)
    {  
        using (var dbConnection = LoadDatabase(connectionFactory))
        using (var transaction = dbConnection.BeginTransaction()) 
        using (var command = dbConnection.CreateCommand())        
        {
            command.Transaction = transaction;
            command.CommandText = commandText;
            command.CommandType = commandType;

            var matchesOnCommand = regParameters.Matches(commandText).Select(m => m.Value).ToList();

            foreach (var param in parameters.Where(r => matchesOnCommand.Contains(r.Key)))
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = param.Key;
                dbParam.Value = ValueOrDbNull(param.Value);
                command.Parameters.Add(dbParam);
            }

            var res = command.ExecuteNonQuery();
            transaction.Commit();

            return res;
        }
    }
}