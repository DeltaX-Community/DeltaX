using Microsoft.Extensions.Configuration;

namespace DeltaX.MemoryMappedRecord
{
    public class KeyValueMemoryConfiguration
    {
        public KeyValueMemoryConfiguration(string sectionName, string configFileName = "appsettings.json")
        {
            var builder = DeltaX.Configuration.Configuration.GetConfigurationBuilder(configFileName);
            var configurationSection = builder.Build().GetSection(sectionName);
            configurationSection?.Bind(this); 
        }

        public KeyValueMemoryConfiguration(IConfigurationSection configurationSection = null)
        {
            configurationSection?.Bind(this);
        }

        public string MemoryName { get; set; } = "DefaultMemory";

        public int IndexCapacity { get; set; }

        public int DataCapacity { get; set; }

        public bool Persistent { get; set; } = false;

    }
}
