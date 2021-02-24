namespace DeltaX.RealTime
{
    using DeltaX.ActivatorFactory;
    using DeltaX.RealTime.Interfaces; 
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

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
                // case "RtConnectorMemoryMapped":
                //     return RtConnectorMemoryMapped.BuildFromFactory(configurationSection, loggerFactory);
                default:
                    string[] assembyAndClass = connectorSection.Type.Split(";");

                    return InstanceCreator.CreateFromStatic<IRtConnector>("BuildFromFactory",
                        assembyAndClass[0].Trim(), assembyAndClass[1].Trim(), new object[] { configurationSection, loggerFactory });
            }
        }
    }
}
