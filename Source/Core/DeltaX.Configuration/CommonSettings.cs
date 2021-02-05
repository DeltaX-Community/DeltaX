namespace DeltaX.Configuration
{
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Diagnostics;

    /// <summary>
    /// BaseConfig, es una configuracion base para derivar otras configuraciones
    /// 
    /// sirve para mantener una porsción de configuracion común y extender 
    /// funcionalidades (opciones) de configuracion en otros archivos
    /// 
    /// </summary>
    public class CommonSettings
    {

        /// <summary>
        /// Base Path para el scada
        /// </summary>
        /// 

        public static bool IsWindowsOs { get { return (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)); } }

        private static string BasePathDefault
        {
            /// FIXME, usar variable de entorno
            get
            {
                if (IsWindowsOs)
                {
                    return @"C:\Users\sima\Antares\repos\deltax";
                }
                return "/home/sima/Antares/deltax";
            }
        }

        public static string BasePath { get; set; } = BasePathDefault;

        public static string BasePathLog { get => Path.Combine(BasePath, @"Logs"); }

        public static string BasePathBin { get => Path.Combine(BasePath, @"Bin"); }

        public static string BasePathConfig { get => Path.Combine(BasePath, @"Cfg"); }

        public static string BasePathData { get => Path.Combine(BasePath, @"Data"); }

        /// <summary>
        /// Nombre del host del archivo general
        /// </summary>
        public string HostName { get; private set; }

        /// <summary>
        /// Nombre del Sector
        /// </summary>
        public string SectorName { get; private set; }

        /// <summary>
        /// Connection String para servidor de base de datos
        /// </summary>
        public string[] ConnectionStrings { get; private set; } = new string[] { "127.0.0.1" };


        /// <summary>
        /// Setea el Nombre del sector, se debe ejecutar antes de cargar la configuracion
        /// </summary>
        /// <param name="sectorName"></param>
        public void SetSectorName(string sectorName)
        {
            SectorName = sectorName;
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


        public static string GetProcesConfigName()
        {
            var process = Process.GetCurrentProcess();
            return $"{process.ProcessName}.json";
        }

        public static string GetPathConfigFile(string configFileName = "common.json")
        {
            var process = Process.GetCurrentProcess();
            var processDirectory = Path.GetDirectoryName(process.MainModule.FileName);

            // Busca junto con el path del proceso
            var commonConfig = Path.Combine(processDirectory, configFileName);
            if (!File.Exists(commonConfig))
            {
                // Busca en el directorio Cfg en el path del proceso
                commonConfig = Path.Combine(processDirectory, "Cfg", configFileName);
                if (!File.Exists(commonConfig))
                {
                    // Busca en el path comun de configuracion
                    commonConfig = Path.Combine(BasePathConfig, configFileName);
                    if (!File.Exists(commonConfig))
                    {
                        return null;
                    }
                }
            }
            return commonConfig;
        }

        public static string GetPathConfigFileByProcessName()
        {
            var appsettings = GetProcesConfigName();

            var configFileName = GetPathConfigFile(appsettings);
            if (string.IsNullOrEmpty(configFileName))
            {
                appsettings = "appsettings.json";
                configFileName = GetPathConfigFile(appsettings);
                if (string.IsNullOrEmpty(configFileName))
                {
                    appsettings = "common.json";
                    configFileName = GetPathConfigFile(appsettings);
                }
            }
            return configFileName;
        }
    } 
}