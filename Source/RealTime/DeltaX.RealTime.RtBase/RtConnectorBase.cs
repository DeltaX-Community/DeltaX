namespace DeltaX.RealTime
{
    using DeltaX.RealTime.Interfaces; 
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class RtConnectorBase : IRtConnector
    {    
        public abstract bool IsConnected { get; }

        public event EventHandler<IRtTag> ValueUpdated;
        public event EventHandler<IRtTag> ValueSetted;
        public event EventHandler<IRtMessage> MessageReceived;
        public event EventHandler<bool> Connected;
        public event EventHandler<bool> Disconnected;

        public abstract IRtTag AddTag(string tagName, string topic, IRtTagOptions options);

        public abstract IRtTag GetTag(string tagName);

        public abstract Task ConnectAsync(CancellationToken? cancellationToken= null);

        public abstract bool Disconnect();

        public abstract bool WriteValue(IRtTag tag, IRtValue value);

        public abstract bool WriteValue(string topic, IRtValue value, IRtTagOptions options = null);

        protected virtual void RaiseOnUpdatedValue(IRtTag tag)
        {
            ValueUpdated?.Invoke(this, tag);
        }

        protected virtual void RaiseOnSetValue(IRtTag tag)
        {
            ValueSetted?.Invoke(this, tag);
        }

        protected virtual void RaiseOnMessageReceive(IRtMessage message)
        {
            MessageReceived?.Invoke(this, message);
        }

        protected virtual void RaiseOnConnect(bool connect)
        {
            Connected?.Invoke(this, connect);
        }

        protected virtual void RaiseOnDisconnect(bool connect)
        {
            Disconnected?.Invoke(this, connect);
        }

        protected void DettachEventHandler()
        {
            ValueUpdated = null;
            ValueSetted = null;
            MessageReceived = null;
            Connected = null;
            Disconnected = null;
        }

        public virtual void Dispose()
        {
            Disconnect();
            DettachEventHandler();
        }

       
    }

}
