using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.Linq;

namespace DeltaX.ActivatorFactory.UnitTest
{
    




    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        public string GetUnitTestBasePath()
        {
            var projectName = $"{Path.DirectorySeparatorChar}DeltaX{Path.DirectorySeparatorChar}";
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var ps = path.Split(projectName);
            return ps.Length > 1 ? Path.Combine(string.Join(projectName, ps.Take(ps.Length - 1)), "DeltaX") : "";
        }

        [Test]
        public void Test1()
        {
            DeltaX.Configuration.CommonSettings.BasePath = GetUnitTestBasePath();
            var basePath = DeltaX.Configuration.CommonSettings.BasePath;
             
            var assemblyName = Path.Combine(basePath, @"Source\Core\DeltaX.Database\bin\Release\net5.0\DeltaX.Database.dll");
            var className = "DeltaX.Database.DbConnectionFactory";

            // Load Assembly only
            var asl = new AssemblyLoader(Path.GetDirectoryName(assemblyName));
            var assembly1 = asl.LoadFromAssemblyName(Path.GetFileName(assemblyName));
            Assert.NotNull(assembly1);

            // Load Assemby and Create Instance 
            var t1 = InstanceCreator.GetType(assemblyName, className);
            var inst = Activator.CreateInstance(t1, new object[] { null, null, null });
            Assert.NotNull(inst);

            /// assemblyName = @"D:\DEV\repos\DeltaX-Community\DeltaX\Bin\DeltaX.TagRuleToDatabase\MySqlConnector.dll";
            /// className = "MySqlConnector.MySqlConnection";
            ///
            /// Console.WriteLine($"Load {assemblyName}!");
            /// var assembly2 = asl.LoadFromAssemblyName(Path.GetFileName(assemblyName));
            /// Assert.NotNull(assembly2);
            ///
            /// var t2 = InstanceCreator.GetType(assemblyName, className);
            /// IDbConnection inst2 = Activator.CreateInstance(t2, new object[] { }) as IDbConnection;
            /// Assert.NotNull(inst2); 
        }
    }
}