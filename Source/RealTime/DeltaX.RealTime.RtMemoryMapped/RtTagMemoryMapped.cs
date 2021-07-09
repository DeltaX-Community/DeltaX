namespace DeltaX.RealTime.RtMemoryMapped
{
    using DeltaX.CommonExtensions;
    using DeltaX.RealTime.Interfaces;
    using System;

    public class RtTagMemoryMapped : RtTagBase, IRtTag
    {
        public RtTagMemoryMapped(RtConnectorMemoryMapped connector, string tagName, string topic, IRtTagOptions options)
        {
            ConnectorMM = connector  ;
            Connector = connector  ;
            TagName = tagName;
            Topic = topic;
            base.Options = options;
            base.Status = connector.ReadTagStatus(Topic);
        }

        public  RtConnectorMemoryMapped ConnectorMM; 

        public override IRtValue Value
        {
            get
            {
                ConnectorMM.ReadAndRaiseTagOnUpdated(this, base.Updated.ToUnixTimestamp());
                return base.Value;
            } 
        }

        public override DateTime Updated
        {
            get
            {
                ConnectorMM.ReadAndRaiseTagOnUpdated(this, base.Updated.ToUnixTimestamp());
                return base.Updated;
            }
        }

        public override bool Status
        {
            get
            {
                ConnectorMM.ReadAndRaiseTagStatusChanged(this, base.Status);
                return base.Status;
            }
        } 

        internal void RaiseOnSetValue(IRtValue value, DateTime? updated = null, bool status = true)
        {
            base.RaiseOnSetValue(Connector, value, updated, status);
        }

        internal void RaiseOnUpdatedValue(IRtValue value, DateTime? updated = null, bool status = true)
        {
            base.RaiseOnUpdatedValue(Connector, value, updated, status);
        }

        internal void RaiseStatusChanged(bool status)
        {
            base.RaiseStatusChanged(Connector, status);
        }

        public override void Dispose()
        {
            ConnectorMM.RemoveTag(this);
            base.Dispose();
        }
    }
}
