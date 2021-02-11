namespace DeltaX.Connections.MqttClientHelper
{ 
    using Microsoft.Extensions.Configuration;
    using System;

    public class MqttConfiguration 
    {
        public MqttConfiguration(string sectionName, string configFileName = "appsettings.json")
        {
            var builder = DeltaX.Configuration.Configuration.GetConfigurationBuilder(configFileName);
            var configurationSection = builder.Build().GetSection(sectionName);
            configurationSection?.Bind(this);
        }

        public MqttConfiguration(IConfigurationSection configurationSection = null)
        {
            configurationSection?.Bind(this); 
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
