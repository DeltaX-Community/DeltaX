namespace DeltaX.Database
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Concurrent;
    using DeltaX.ActivatorFactory;

    public class DatabaseManager 
    { 
        private IConfiguration configuration;
        private readonly ILoggerFactory loggerFactory;
        private ConcurrentDictionary<string, IDatabaseBase> databases; 

        public DatabaseManager(IConfiguration configuration, ILoggerFactory loggerFactory)
        { 
            this.configuration = configuration;
            this.loggerFactory = loggerFactory;
            databases = new ConcurrentDictionary<string, IDatabaseBase>();
        }

        public IDatabaseBase GetDatabase(string connectionFactory)
        {
            if (!databases.TryGetValue(connectionFactory, out _))
            {
                databases[connectionFactory] = GetDbConnectionFactory(connectionFactory);
            }

            return databases[connectionFactory];
        }

        private IDatabaseBase GetDbConnectionFactory(string connectionFactory = "DefaultDatabaseConnection")
        {
            var connectionSection = new DatabaseConnectionConfiguration();
            configuration.GetSection(connectionFactory).Bind(connectionSection);

            string[] assembyAndClass = connectionSection.Type.Split(";");
            var type = InstanceCreator.GetType(assembyAndClass[0].Trim(), assembyAndClass[1].Trim());

            return DatabaseBase.Build(type, new[] { connectionSection.ConnectionString }, loggerFactory);
        }
    }
}
