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

    public class RtConnectorMqtt : RtConnectorBase, IRtConnector
    {
        MqttClientHelper mqttClient;
        HashSet<RtTagMqtt> rtTags;
        private ILogger logger;


        public static IRtConnector Build(
            string sectionName = "Mqtt",
            string configFileName = "common.json",
            ILoggerFactory loggerFactory = null)
        {
            var config = new MqttConfiguration(sectionName, configFileName);
            var mqttClient = new MqttClientHelper(config);
            return new RtConnectorMqtt(mqttClient, loggerFactory);
        }

        public static IRtConnector Build(
            IConfiguration configuration,
            string sectionName = "Mqtt",
            ILoggerFactory loggerFactory = null)
        {
            var config = new MqttConfiguration(configuration, sectionName);
            var mqttClient = new MqttClientHelper(config);
            return new RtConnectorMqtt(mqttClient, loggerFactory);
        }

        public static IRtConnector Build(
            MqttClientHelper mqttClient,
            ILoggerFactory loggerFactory = null)
        {
            return new RtConnectorMqtt(mqttClient, loggerFactory);
        }

        protected RtConnectorMqtt(
            MqttClientHelper mqttClient,
            ILoggerFactory loggerFactory = null)
        {
            loggerFactory ??= Configuration.DefaultLoggerFactory;

            this.mqttClient = mqttClient;
            this.logger = loggerFactory.CreateLogger($"{nameof(RtConnectorMqtt)}");

            this.mqttClient.OnConnectionChange += MqttOnConnectionChange;
            this.mqttClient.Client.MqttMsgPublishReceived += MqttOnMessageReceive;

            rtTags = new HashSet<RtTagMqtt>();
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
                lock (rtTags)
                {
                    foreach (var tag in rtTags)
                    {
                        tag.RaiseStatusChanged(IsConnected);
                    }
                }
            }
        }

        private void MqttOnMessageReceive(object sender, MqttMsgPublishEventArgs e)
        {  
            RtTagMqtt[] tags;
            IRtMessage msg = RtMessage.Create(e.Topic, RtValue.Create(e.Message), e);

            lock (rtTags)
            {
                tags = rtTags.Where(tt => tt.Topic == msg.Topic).ToArray();
            }

            foreach (var tag in tags)
            {
                tag.RaiseOnUpdatedValue(msg.Value);
                RaiseOnUpdatedValue(tag);
            }

            RaiseOnMessageReceive(msg);
        }
        

        public override bool IsConnected => this.mqttClient.IsConnected;

        public override IEnumerable<string> TagNames
        {
            get
            {
                lock (rtTags) return rtTags.Select(t => t.TagName);
            }
        }

        public override IRtTag AddTag(string tagName, string topic, IRtTagOptions options) 
        { 
            RtTagMqtt t = new RtTagMqtt(this, tagName, topic, (options as RtTagMqttOptions));
            lock (rtTags)
            {                
                rtTags.Add(t);
            }
            Subscribe(t);
            logger?.LogInformation("AddTag TagName:{0}", t.TagName);
            return t;
        }

        public override IRtTag GetTag(string tagName)
        {
            lock (rtTags)
            {
                return rtTags.FirstOrDefault(t => t.TagName == tagName);
            }
        }

        public void RemoveTag(IRtTag tag)
        {
            lock (rtTags)
            {
                rtTags.Remove(tag as RtTagMqtt);
            }
            logger?.LogInformation("RemoveTag TagName:{0}", tag.TagName);
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
            lock (rtTags)
            {
                var tagsByTopic = rtTags.ToDictionary(t => t.Topic, t => t);
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
                        lock (rtTags)
                        {
                            foreach (var _tag in rtTags.Where(t => t.Topic == tag.Topic))
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

