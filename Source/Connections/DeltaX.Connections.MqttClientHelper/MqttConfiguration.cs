namespace DeltaX.Connections.MqttClientHelper
{ 
    using Microsoft.Extensions.Configuration;
    using System;

    public class MqttConfiguration 
    {
        public MqttConfiguration(string sectionName = "Mqtt", string configFileName = "appsettings.json")
        {
            var builder = DeltaX.Configuration.Configuration.GetConfigurationBuilder(configFileName);
            var configurationSection = builder.Build().GetSection(sectionName);
            Initialize(configurationSection);
        } 

        public MqttConfiguration(IConfiguration configuration, string section)
        {
            var configurationSection = configuration.GetSection(section);
            Initialize(configurationSection);
        }

        public MqttConfiguration(IConfigurationSection configurationSection = null)
        {
            Initialize(configurationSection);
        }

        private void Initialize(IConfigurationSection configurationSection)
        {
            if (configurationSection != null)
            {
                ClientId = configurationSection.GetValue("ClientId", ClientId);
                Host = configurationSection.GetValue("Host", Host);
                Port = configurationSection.GetValue("Port", Port);
                Secure = configurationSection.GetValue("Secure", Secure);
                Username = configurationSection.GetValue("Username", Username);
                Password = configurationSection.GetValue("Password", Password);
                ReconnectDealy = configurationSection.GetValue("ReconnectDealy", ReconnectDealy);
            }
        }

        public string ClientId { get; set; } = Guid.NewGuid().ToString("N");

        public string Host { get; set; } = "127.0.0.1";

        public int Port { get; set; } = 1883;

        public bool Secure { get; set; } = false;

        public string Username { get; set; } = null;

        public string Password { get; set; } = null;

        public int ReconnectDealy { get; set; } = 1000;

    }
}
