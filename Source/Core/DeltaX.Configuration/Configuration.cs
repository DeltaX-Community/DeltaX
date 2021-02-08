﻿namespace DeltaX.Configuration
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
                defaultLoggerFactory ??= LoggerConfiguration.GetSerilogLoggerFactory();
                return defaultLoggerFactory;
            }
        }

        public static ILogger DefaultLogger
        {
            get
            {
                defaultLogger ??= DefaultLoggerFactory.CreateLogger("");
                return defaultLogger;
            }
        }

        public static void SetDefaultLogger( )
        {
            LoggerConfiguration.SetSerilog( );
        }

    }
}