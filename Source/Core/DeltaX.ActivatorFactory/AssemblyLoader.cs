
namespace DeltaX.ActivatorFactory
{
    using Microsoft.Extensions.DependencyModel;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;

    public class AssemblyLoader : AssemblyLoadContext
    {
        private string folderPath;

        public AssemblyLoader(string folderPath)
        {
            this.folderPath = folderPath;
        }

        public Assembly LoadFromAssemblyName(string assemblyFileName)
        {
            var assembyName = assemblyFileName.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)
               ? assemblyFileName.Substring(0, assemblyFileName.Length - 4)
               : assemblyFileName;

            return LoadFromAssemblyName(new AssemblyName(assembyName));
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var deps = DependencyContext.Default;
            var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
            if (res.Count > 0)
            {
                return Assembly.Load(assemblyName);
            }
            else
            {
                var fullPath = string.IsNullOrEmpty(folderPath)
                    ? $"{assemblyName.Name}.dll"
                    : Path.Combine(folderPath, $"{assemblyName.Name}.dll");
                var apiApplicationFileInfo = new FileInfo(fullPath);
                if (File.Exists(apiApplicationFileInfo.FullName))
                {
                    Console.WriteLine($"Load Assemby: {apiApplicationFileInfo.FullName}");
                    return this.LoadFromAssemblyPath(apiApplicationFileInfo.FullName);
                }
            }
            return Assembly.Load(assemblyName);
        }
    }
}
