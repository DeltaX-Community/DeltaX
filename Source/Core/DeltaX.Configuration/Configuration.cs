namespace DeltaX.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Json;
    using System.IO;

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


       
    }
}