namespace DeltaX.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using DeltaX.Configuration.Serilog;

    public class Configuration
    {
        public static IConfigurationBuilder GetConfigurationBuilder(string configFile = null, bool optional = false, bool reloadOnChange = false)
        {
            string configFileName;
            if (string.IsNullOrEmpty(configFile))
            {
                configFileName = CommonSettings.GetPathConfigFileByProcessName();
            }
            else
            {
                configFileName = CommonSettings.GetPathConfigFile(configFile);
            }

            if (string.IsNullOrEmpty(configFileName))
                return null;

            return new ConfigurationBuilder().AddJsonFile(configFileName, optional, reloadOnChange);
        }

        static ILoggerFactory defaultLoggerFactory;
        static ILogger defaultLogger;

        public static ILoggerFactory DefaultLoggerFactory
        {
            get
            {
                defaultLoggerFactory = defaultLoggerFactory ?? LoggerConfiguration.GetSerilogLoggerFactory();
                return defaultLoggerFactory;
            }
            set
            {
                defaultLoggerFactory = value;
            }
        }

        public static ILogger DefaultLogger
        {
            get
            {
                defaultLogger = defaultLogger ?? DefaultLoggerFactory.CreateLogger("");
                return defaultLogger;
            }
            set
            {
                defaultLogger = value;
            }
        }

        public static void SetDefaultLogger(IConfiguration configuration = null)
        {
            LoggerConfiguration.SetSerilog(null, configuration);
            DefaultLoggerFactory = LoggerConfiguration.GetSerilogLoggerFactory();
        }
    }
}