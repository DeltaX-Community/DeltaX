namespace DeltaX.RealTime.RtMqtt
{
    using DeltaX.RealTime.Interfaces;

    class RtTagMqtt : RtTagBase, IRtTag
    {
        public RtTagMqtt(IRtConnector connector, string tagName, string topic, RtTagMqttOptions options)
        {
            Connector = connector;
            TagName = tagName;
            Topic = topic;
            base.Options = options ?? new RtTagMqttOptions();
        }

        public new RtTagMqttOptions Options => (RtTagMqttOptions)base.Options;

        internal void RaiseOnSetValue(IRtValue value)
        {
            base.RaiseOnSetValue(Connector, value);
        }

        internal void RaiseOnUpdatedValue(IRtValue value)
        {
            base.RaiseOnUpdatedValue(Connector, value);
        }

        internal void RaiseStatusChanged(bool status)
        {
            base.RaiseStatusChanged(Connector, status);
        } 

        public override void Dispose()
        {
            (Connector as RtConnectorMqtt).RemoveTag(this);
            base.Dispose();
        }
    }
}
