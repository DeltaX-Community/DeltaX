namespace DeltaX.Configuration
{
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// BaseConfig, es una configuracion base para derivar otras configuraciones
    /// 
    /// sirve para mantener una porsción de configuracion común y extender 
    /// funcionalidades (opciones) de configuracion en otros archivos
    /// 
    /// </summary>
    public class CommonSettings
    {
        private static IConfiguration commonConfiguration;
        private static string basePath = BasePathDefault;

        public static bool IsWindowsOs
        {
            get
            {
                return (RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            }
        }

        /// <summary>
        /// Base Path para el scada
        /// </summary>
        private static string BasePathDefault
        {
            /// FIXME, usar variable de entorno
            get
            {
                if (IsWindowsOs)
                {
                    return @"C:\DeltaX";
                }
                return "/home/deltax";
            }
        }      


        public static string BasePath
        {
            get
            {
                return basePath;
            }
            set
            {
                basePath = value;
                commonConfiguration = null;
            }
        }

        public static string BasePathLog { get => Path.Combine(BasePath, @"Logs"); }

        public static string BasePathBin { get => Path.Combine(BasePath, @"Bin"); }

        public static string BasePathConfig { get => Path.Combine(BasePath, @"Cfg"); }

        public static string BasePathData { get => Path.Combine(BasePath, @"Data"); }

        public static string CommonConfigName => Path.Combine(BasePathConfig, "common.json");

        public static string DefaultDateTimeFormat { get; set; } = "o";// "yyyy/MM/dd HH:mm:ss.fff";
             

        public static IConfiguration CommonConfiguration
        {
            get
            {
                if (commonConfiguration != null)
                {
                    return commonConfiguration;
                }

                var basePathCommon = Path.Combine(BasePathConfig, "common.json");
                if (!File.Exists(basePathCommon))
                {
                    return null;
                }

                var builder = new ConfigurationBuilder().AddJsonFile(basePathCommon, optional: false, reloadOnChange: false);
                commonConfiguration = builder.Build();
                return commonConfiguration;
            }
        }


        /// <summary>
        /// Setea el directorio estandar de trabajo
        /// </summary>
        public static void SetCurrentDirectoryBaseBin()
        {
            Directory.SetCurrentDirectory(BasePathBin);
        }

        public static void SetCurrentDirectoryFromExecutable()
        {
            var process = Process.GetCurrentProcess();
            var processDirectory = Path.GetDirectoryName(process.MainModule.FileName);
            Directory.SetCurrentDirectory(processDirectory);
        }
        
        public static void SetBasePathFromExecutable()
        {
            var process = Process.GetCurrentProcess();
            var processDirectory = Path.GetDirectoryName(process.MainModule.FileName);
            BasePath = processDirectory;
        }
        
        public static string GetProcesConfigName()
        {
            var process = Process.GetCurrentProcess();
            return $"{process.ProcessName}.json";
        }

        public static string GetPathConfigFile(string configFileName = "common.json")
        {           
            if (File.Exists(configFileName))
            {
                return configFileName;
            }

            var process = Process.GetCurrentProcess();
            var processDirectory = Path.GetDirectoryName(process.MainModule.FileName);

            // Busca junto con el path del proceso
            var commonConfig = Path.Combine(processDirectory, configFileName);
            if (File.Exists(commonConfig))
            {
                return commonConfig;
            }
            
            // Busca en el directorio Cfg en el path del proceso
            commonConfig = Path.Combine(processDirectory, "Cfg", configFileName);
            if (File.Exists(commonConfig))
            {
                return commonConfig;
            }

            // Busca en el path comun de configuracion
            commonConfig = Path.Combine(BasePathConfig, configFileName);
            if (File.Exists(commonConfig))
            {
                return commonConfig;
            }

            return null;
        }

        public static string GetPathConfigFileByProcessName()
        {
            var configFileName = GetProcesConfigName();
            configFileName = GetPathConfigFile(configFileName);
            if (string.IsNullOrEmpty(configFileName))
            {
                configFileName = "appsettings.json";
                configFileName = GetPathConfigFile(configFileName);
                if (string.IsNullOrEmpty(configFileName))
                {
                    configFileName = "common.json";
                    configFileName = GetPathConfigFile(configFileName);
                }
            }
            return configFileName;
        }
    }
}