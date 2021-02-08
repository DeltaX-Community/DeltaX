namespace DeltaX.RealTime.RtMqtt
{
    using DeltaX.RealTime.Interfaces;

    public enum RtMqttMsgQosLevels : byte
    {
        QOS_LEVEL_AT_MOST_ONCE = 0,
        QOS_LEVEL_AT_LEAST_ONCE = 1,
        QOS_LEVEL_EXACTLY_ONCE = 2,
        QOS_LEVEL_GRANTED_FAILURE = 128,
    }
     
    public class RtTagMqttOptions : IRtTagOptions
    {
        public bool retain { get; set; } = true;
        public RtMqttMsgQosLevels qosLevels { get; set; } = RtMqttMsgQosLevels.QOS_LEVEL_AT_MOST_ONCE;
    }
}
