namespace DeltaX.RealTime
{
    using DeltaX.RealTime.Interfaces;

    public class RtMessage : IRtMessage
    {
        public static IRtMessage Create(string topic, IRtValue value, object raw)
        {
            return new RtMessage(topic, value, raw);
        }

        public string Topic { get; private set; }

        public IRtValue Value { get; private set; }

        public object Raw { get; private set; }

        private RtMessage(string topic, IRtValue value, object raw)
        {
            Topic = topic;
            Value = value;
            Raw = raw;
        } 

    }
}
