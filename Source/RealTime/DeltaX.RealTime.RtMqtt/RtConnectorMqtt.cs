namespace DeltaX.RealTime.RtMqtt
{
    using DeltaX.Configuration;
    using DeltaX.Connections.MqttClientHelper;
    using DeltaX.RealTime.Interfaces;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging; 
    using System.Collections.Generic;
    using System.Linq; 
    using System.Threading;
    using System.Threading.Tasks;
    using uPLibrary.Networking.M2Mqtt.Messages;

    public class RtConnectorMqtt : RtConnectorBase
    {
        MqttClientHelper mqttClient;
        HashSet<RtTagMqtt> Tags = new HashSet<RtTagMqtt>();
        private ILogger logger;


        public static IRtConnector Build(string sectionName = "Mqtt", string configFileName = "common.json", ILoggerFactory loggerFactory = null)
        {
            var config = new MqttConfiguration(sectionName, configFileName);
            var mqttClient = new MqttClientHelper(config);
            return new RtConnectorMqtt(mqttClient, loggerFactory);
        }

        public static IRtConnector Build(IConfiguration configuration, string sectionName = "Mqtt", ILoggerFactory loggerFactory = null)
        {
            var config = new MqttConfiguration(configuration, sectionName);
            var mqttClient = new MqttClientHelper(config);
            return new RtConnectorMqtt(mqttClient, loggerFactory);
        }

        public static IRtConnector Build(MqttClientHelper mqttClient, ILoggerFactory loggerFactory = null)
        {
            return new RtConnectorMqtt(mqttClient, loggerFactory);
        }

        protected RtConnectorMqtt(MqttClientHelper mqttClient, ILoggerFactory loggerFactory = null)
        {
            loggerFactory ??= Configuration.DefaultLoggerFactory;

            this.mqttClient = mqttClient;
            this.logger = loggerFactory.CreateLogger($"{nameof(RtConnectorMqtt)}");

            this.mqttClient.OnConnectionChange += MqttOnConnectionChange;
            this.mqttClient.Client.MqttMsgPublishReceived += MqttOnMessageReceive;
        }

        private void MqttOnConnectionChange(object sender, bool isConnected)
        {
            if(isConnected)
            {
                logger?.LogInformation("MqttOnConnect");
                SubscribeAll();
                RaiseOnConnect(IsConnected);
            }
            else
            {
                logger?.LogWarning("MqttOnDisconnect");
                RaiseOnDisconnect(IsConnected);
                lock (Tags)
                {
                    foreach (var tag in Tags)
                    {
                        tag.RaiseOnDisconnect(IsConnected);
                    }
                }
            }
        } 


        private void MqttOnMessageReceive(object sender, MqttMsgPublishEventArgs e)
        {  
            RtTagMqtt[] tags;
            IRtMessage msg = RtMessage.Create(e.Topic, RtValue.Create(e.Message), e);

            lock (Tags)
            {
                tags = Tags.Where(tt => tt.Topic == msg.Topic).ToArray();
            }

            foreach (var tag in tags)
            {
                tag.RaiseOnUpdatedValue(msg.Value);
                RaiseOnUpdatedValue(tag);
            }

            RaiseOnMessageReceive(msg);
        }
        

        public override bool IsConnected => this.mqttClient.IsConnected;

        public override IRtTag AddTag(string tagName, string topic, IRtTagOptions options) 
        { 
            RtTagMqtt t = new RtTagMqtt(this, tagName, topic, (options as RtTagMqttOptions));
            lock (Tags)
            {                
                Tags.Add(t);
            }
            Subscribe(t);
            logger?.LogInformation("AddTag TagName:{0}", t.TagName);
            return t;
        }

        public override IRtTag GetTag(string tagName)
        {
            lock (Tags)
            {
                return Tags.Where(t => t.TagName == tagName).FirstOrDefault();
            }
        }

        public void RemoveTag(IRtTag tag)
        {
            try
            {
                lock (Tags)
                {
                    Tags.Remove(tag as RtTagMqtt);
                }
                logger?.LogInformation("RemoveTag TagName:{0}", tag.TagName);
            }
            catch { }
        }

        private void Subscribe(RtTagMqtt tag)
        {
            if (IsConnected)
            {
                mqttClient.Client.Subscribe(new[] { tag.Topic },
                    new[] { (byte)tag.Options.qosLevels});
            }
        }

        private void SubscribeAll()
        {
            string[] topics;
            byte[] qosLevel;

            // Subscribe distinct topics only
            lock (Tags)
            {
                var tagsByTopic = Tags.ToDictionary(t => t.Topic, t => t);
                topics = tagsByTopic.Keys.ToArray();
                qosLevel = tagsByTopic.Values.Select(t => (byte)t.Options.qosLevels ).ToArray();
            }

            logger?.LogDebug("SubscribeAll topics.Count:{0} Topics:[{1}]", topics.Count(), string.Join(", ", topics));
            if (topics.Any())
            {
                mqttClient.Client.Subscribe(topics, qosLevel);
            }
        }

        public override Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            logger?.LogDebug("ConnectAsync");
            return mqttClient.ConnectAsync(cancellationToken);
        }

        public override bool Disconnect()
        {
            logger?.LogDebug("Disconnect");
            mqttClient?.Disconnect();
            return true;
        }

        public override bool WriteValue(IRtTag tag, IRtValue value)
        {
            try
            {
                if (mqttClient.IsConnected)
                {
                    RtTagMqttOptions options = tag.Options as RtTagMqttOptions;
                    var writed = mqttClient.Client.Publish(tag.Topic, value.Binary, (byte)options.qosLevels, options.retain);
                    if (writed > 0)
                    {
                        lock (Tags)
                        {
                            foreach (var _tag in Tags.Where(t => t.Topic == tag.Topic))
                            {
                                _tag.RaiseOnSetValue(value);
                            }
                        }

                        RaiseOnSetValue(tag);

                        logger?.LogDebug("WriteValue TagName:{0} return:{1}", tag.TagName, true);
                        return true;
                    }
                }
            }
            catch { }
            logger?.LogWarning("WriteValue TagName:{0} return:{1}", tag.TagName, false);
            return false;
        }

        public override bool WriteValue(string topic, IRtValue value, IRtTagOptions options = null)
        {
            return WriteValue(new RtTagMqtt(this, topic, topic, options as RtTagMqttOptions), value);
        }

        public override void Dispose()
        {
            Disconnect();
            base.Dispose();
        }  
    }
}

