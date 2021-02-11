using DeltaX.RealTime.Interfaces;
using DeltaX.RealTime.RtMemoryMapped;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace DeltaX.RealTime
{

    public class RealTimeConnectorSection
    {  
        public string Type { get; set; }
        public string SectionName { get; set; } 
    }

    public class RtConnectorFactory
    {
        private readonly IConfiguration configuration;
        private readonly ILoggerFactory loggerFactory;

        public RtConnectorFactory(IConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            this.configuration = configuration;
            this.loggerFactory = loggerFactory;
        }

        public IRtConnector GetConnector(string realTimeConnectorSectionName = "DefaultRealTimeConnector")
        {
            var connectorSection = new RealTimeConnectorSection();
            configuration.GetSection(realTimeConnectorSectionName).Bind(connectorSection);
            var configurationSection = configuration.GetSection(connectorSection.SectionName);

            switch (connectorSection.Type)
            {
                case "RtConnectorMemoryMapped":
                    return RtConnectorMemoryMapped.BuildFromFactory(configurationSection, loggerFactory);
                default:
                    string[] assembyAndClass = connectorSection.Type.Split(";");
                    return Build(assembyAndClass[0].Trim(), assembyAndClass[1].Trim(), new object[] { configurationSection, loggerFactory });
            }
        }


        IRtConnector Build(string assemmblyName, string className, params object[] parameters)
        { 
            Assembly a = Assembly.Load(assemmblyName); 
            Type myType = a.GetType(className);
             
            MethodInfo methodBuild = myType.GetMethod("BuildFromFactory", BindingFlags.Public | BindingFlags.Static);

            return (IRtConnector)methodBuild.Invoke(null, parameters);
        }
    }
}
