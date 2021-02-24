
namespace DeltaX.ActivatorFactory
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class InstanceCreator
    {
        static ILogger logger => DeltaX.Configuration.Configuration.DefaultLogger;

        public static TInterface Create<TInterface>(
            string assemblyString,
            string className,
            params object[] parameters)
        {
            var asl = new AssemblyLoader(Path.GetDirectoryName(assemblyString));
            var assembly = asl.LoadFromAssemblyName(Path.GetFileName(assemblyString));

            var type = assembly.GetType(className);

            if (type == null)
            {
                type = assembly.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.FullName.EndsWith(className));
            }

            if (type != null && type.GetInterfaces().Contains(typeof(TInterface)))
            {
                var parmTypes = parameters?.Select(p => p.GetType()).ToArray();
                var constructor = type.GetConstructor(parmTypes ?? Type.EmptyTypes);
                if (constructor != null)
                {
                    return (TInterface)constructor.Invoke(parameters);
                }
            }

            throw new Exception($"Unable to instantiate type {className} from assembly {assemblyString}");
        }


        public static Type GetType(
            string assemblyString,
            string className)
        {
            var asl = new AssemblyLoader(Path.GetDirectoryName(assemblyString));
            var assembly = asl.LoadFromAssemblyName(Path.GetFileName(assemblyString));

            var type = assembly.GetType(className);

            if (type == null)
            {
                type = assembly.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.FullName.EndsWith(className));
            }
            return type;
        }

        public static TInterface CreateFromStatic<TInterface>(
            string staticMethodConstructor,
            string assemblyString,
            string className,
            params object[] parameters)
        {
            var asl = new AssemblyLoader(Path.GetDirectoryName(assemblyString));
            var assembly = asl.LoadFromAssemblyName(Path.GetFileName(assemblyString));

            var type = assembly.GetType(className);

            if (type == null)
            {
                type = assembly.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.FullName.EndsWith(className));
            }

            if (type != null && type.GetInterfaces().Contains(typeof(TInterface)))
            {
                MethodInfo methodBuild = type.GetMethod(staticMethodConstructor, BindingFlags.Public | BindingFlags.Static);

                return (TInterface)methodBuild.Invoke(null, parameters);
            }

            throw new Exception($"Unable to instantiate type {className} from assembly {assemblyString}");
        }

        public static TInterface TryCreateFromInterface<TInterface>(
            string assemblyString,
            params object[] parameters)
        {
            var asl = new AssemblyLoader(Path.GetDirectoryName(assemblyString));
            var assembly = asl.LoadFromAssemblyName(Path.GetFileName(assemblyString));

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract)
                {
                    if (type.GetInterfaces().Contains(typeof(TInterface)))
                    {
                        var parmTypes = parameters?.Select(p => p.GetType()).ToArray();
                        var constructor = type.GetConstructor(parmTypes ?? Type.EmptyTypes);
                        if (constructor != null)
                        {
                            try
                            {
                                return (TInterface)constructor.Invoke(parameters);
                            }
                            catch (Exception e)
                            {
                                logger?.LogError(e, $"TryGetInstance with constructor {constructor}");
                            }
                        }
                    }
                }
            }

            return default(TInterface);
        }
    }
}
