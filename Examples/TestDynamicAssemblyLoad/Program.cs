using DeltaX.ActivatorFactory; 
using System;
using System.IO;
using System.Linq;
using System.Reflection; 

namespace TestDynamicAssemblyLoad
{ 

    class Program
    {
        private static void PrintTypes(Assembly assembly)
        {
            foreach (TypeInfo type in assembly.DefinedTypes)
            {
                Console.WriteLine(type.Name);
                foreach (PropertyInfo property in type.DeclaredProperties)
                {
                    string attributes = string.Join(
                        ", ",
                        property.CustomAttributes.Select(a => a.AttributeType.Name));

                    if (!string.IsNullOrEmpty(attributes))
                    {
                        Console.WriteLine("    [{0}]", attributes);
                    }
                    Console.WriteLine("    {0} {1}", property.PropertyType.Name, property.Name);
                }
            }
        }

        static void Main(string[] args)
        {
            var assemblyName = @"D:\DEV\repos\DeltaX-Community\DeltaX\Bin\DeltaX.TagRuleToDatabase\DeltaX.Database.dll"; 
            var className = "DeltaX.Database.DbConnectionFactory";

            Console.WriteLine($"Load {assemblyName}!");
            var asl = new AssemblyLoader(Path.GetDirectoryName(assemblyName));
            var res1 = asl.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(assemblyName)));
            // PrintTypes(res1);
            var t1 = InstanceCreator.GetType(assemblyName, className);
            var inst = Activator.CreateInstance(t1, new object[] { null, null, null });


            assemblyName = @"D:\DEV\repos\DeltaX-Community\DeltaX\Bin\DeltaX.TagRuleToDatabase\MySqlConnector.dll";
            className = "MySqlConnector.MySqlConnection";

            Console.WriteLine($"Load {assemblyName}!");
            var res2 = asl.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(assemblyName)));
            // PrintTypes(res2);
            var t2 = InstanceCreator.GetType(assemblyName, className);
            var inst2 = Activator.CreateInstance(t2, new object[] { });

        }
    }
}
