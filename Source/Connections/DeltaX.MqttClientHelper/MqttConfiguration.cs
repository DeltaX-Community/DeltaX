namespace DeltaX.Connections.MqttClientHelper
{ 
    using Microsoft.Extensions.Configuration;


    public class MqttConfiguration 
    {
        public MqttConfiguration(string section, string configFileName = null)
        {
            var builder = DeltaX.Configuration.Configuration.GetConfigurationBuilder(configFileName);
            var configurationSection = builder.Build().GetSection(section);
            Initialize(configurationSection);
        } 

        public MqttConfiguration(IConfiguration configuration, string section)
        {
            var configurationSection = configuration.GetSection(section);
            Initialize(configurationSection);
        }

        public MqttConfiguration(IConfigurationSection configurationSection)
        {
            Initialize(configurationSection);
        }

        private void Initialize(IConfigurationSection configurationSection)
        {
            ClientId = configurationSection.GetValue("ClientId", ClientId);
            Host = configurationSection.GetValue("Host", Host);
            Port = configurationSection.GetValue("Port", Port);
            Secure = configurationSection.GetValue("Secure", Secure);
            Username = configurationSection.GetValue("Username", Username);
            Password = configurationSection.GetValue("Password", Password);
            ReconnectDealy = configurationSection.GetValue("ReconnectDealy", ReconnectDealy);
        }

        public string ClientId { get; set; } = null;

        public string Host { get; set; } = "127.0.0.1";

        public int Port { get; set; } = 1883;

        public bool Secure { get; set; } = false;

        public string Username { get; set; } = null;

        public string Password { get; set; } = null;

        public int ReconnectDealy { get; set; } = 1000;

    }
}
