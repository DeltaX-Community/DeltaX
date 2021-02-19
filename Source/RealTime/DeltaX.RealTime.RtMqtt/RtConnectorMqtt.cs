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
        private string prefix;
        private string[] extraTopicsSbscribe;
        private HashSet<string> extraTopics;
        private bool autoAddExtraTopics = false;


        public static IRtConnector Build(
            string sectionName = "Mqtt",
            string configFileName = "common.json",
            ILoggerFactory loggerFactory = null,
            string prefix = null,
            string[] extraTopicsSbscribe = null)
        {
            var config = new MqttConfiguration(sectionName, configFileName);
            var mqttClient = new MqttClientHelper(config);
            return new RtConnectorMqtt(mqttClient, loggerFactory, prefix, extraTopicsSbscribe);
        } 

        public static IRtConnector Build(
            MqttClientHelper mqttClient,
            ILoggerFactory loggerFactory = null,
            string prefix = null,
            string[] extraTopicsSbscribe = null)
        {
            return new RtConnectorMqtt(mqttClient, loggerFactory, prefix, extraTopicsSbscribe);
        }

        public static IRtConnector BuildFromFactory(
            IConfigurationSection configuration,
            ILoggerFactory loggerFactory = null)
        {
            var config = new MqttConfiguration(configuration);
            var mqttClient = new MqttClientHelper(config);
            var prefix = configuration?.GetValue<string>("Prefix", "");
            var extraTopicsSbscribe = configuration?.GetSection("ExtraTopicsSbscribe")?.Get<string[]>(); 
            return new RtConnectorMqtt(mqttClient, loggerFactory, prefix, extraTopicsSbscribe);
        }

        protected RtConnectorMqtt(
            MqttClientHelper mqttClient,
            ILoggerFactory loggerFactory = null,
            string prefix = null, 
            string[] extraTopicsSbscribe = null)
        {
            loggerFactory ??= Configuration.DefaultLoggerFactory;

            this.mqttClient = mqttClient;
            this.prefix = prefix ?? "";
            this.logger = loggerFactory.CreateLogger($"{nameof(RtConnectorMqtt)}");
            this.extraTopicsSbscribe = extraTopicsSbscribe ?? new string[0];
            this.extraTopics = new HashSet<string>();

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

                if (!extraTopics.Contains(e.Topic))
                {
                    extraTopics.Add(e.Topic);
                }
            }

            foreach (var tag in tags)
            {
                tag.RaiseOnUpdatedValue(msg.Value);
                RaiseOnUpdatedValue(tag);
            } 

            if (autoAddExtraTopics && !tags.Any() && extraTopicsSbscribe.Any())
            {
                var tag = (RtTagMqtt)AddTag(e.Topic, e.Topic, null);
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
        
        public override IEnumerable<string> KnownTopics
        {
            get
            {
                var prefixLen = string.IsNullOrEmpty(prefix) ? 0 : prefix.Length;
                lock (rtTags) return extraTopics
                        .Union(rtTags.Select(t => t.Topic))
                        .Select(t => prefixLen > 0 && t.StartsWith(prefix) ? t.Substring(prefixLen) : t)
                        .ToHashSet();
            }
        }

        public override IRtTag AddTag(string tagName, string topic, IRtTagOptions options)
        {
            var tag = GetTag(tagName);
            if (tag != null)
            {
                return tag;
            }

            RtTagMqtt t = new RtTagMqtt(this, tagName, $"{prefix}{topic}", (options as RtTagMqttOptions));
            lock (rtTags)
            {
                rtTags.Add(t);
            }
            Subscribe(t);
            logger?.LogInformation("AddTag TagName:{0} prefix:{1}", t.TagName, prefix);
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
                var tagsByTopic = rtTags.GroupBy(t => t.Topic).Select(g => g.First()).ToList();
                topics = tagsByTopic.Select(t => t.Topic).ToArray();
                qosLevel = tagsByTopic.Select(t => (byte)t.Options.qosLevels).ToArray();
            }

            logger?.LogDebug("SubscribeAll topics.Count:{0} Topics:[{1}]", topics.Count(), string.Join(", ", topics));
            if (topics.Any())
            {
                mqttClient.Client.Subscribe(topics, qosLevel);
            }

            if (extraTopicsSbscribe.Any())
            {
                logger?.LogDebug("SubscribeAll ExtraTopicsSbscribe Count:{0} Topics:[{1}]", 
                    extraTopicsSbscribe.Count(), string.Join(", ", extraTopicsSbscribe));
                qosLevel = extraTopicsSbscribe.Select(t => (byte)RtMqttMsgQosLevels.QOS_LEVEL_AT_MOST_ONCE).ToArray();
                mqttClient.Client.Subscribe(extraTopicsSbscribe, qosLevel);
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

                        logger?.LogDebug("WriteValue TagName:{0} Value:{1} return:{2}", tag.TagName, tag.Value.Text, true);
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
            return WriteValue(new RtTagMqtt(this, topic, $"{prefix}{topic}", options as RtTagMqttOptions), value);
        }

        public override void Dispose()
        {
            Disconnect();
            base.Dispose();
        }  
    }
}

