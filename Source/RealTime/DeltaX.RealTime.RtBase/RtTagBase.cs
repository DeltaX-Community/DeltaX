namespace DeltaX.RealTime
{
    using DeltaX.CommonExtensions;
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json;

    public abstract class RtTagBase : IRtTag, IDisposable, IEqualityComparer<RtTagBase>
    {
        protected static IRtValue valueNull = RtValue.Create(string.Empty);
        protected bool _status;

        public virtual event EventHandler<IRtTag> ValueUpdated;
        public virtual event EventHandler<IRtTag> ValueSetted;
        public virtual event EventHandler<IRtTag> StatusChanged;
           
        public virtual IRtConnector Connector { get; protected set; }

        public virtual string TagName { get; protected set; }

        public virtual string Topic { get; protected set; }         

        public virtual IRtTagOptions Options{ get; protected set; }

        public virtual IRtValue Value { get; protected set; } = valueNull;

        public virtual DateTime Updated { get; protected set; } = DateTime.MinValue;         
        
        public virtual bool Status
        {
            get
            {
                if (Connector != null && (!Connector.IsConnected && _status))
                {
                    Status = false;
                }
                return _status;
            }
            protected set
            {
                if (_status != value)
                {
                    StatusChanged?.Invoke(this, this);
                    _status = value;
                }
            }
        }

        public virtual bool Set(IRtValue value)
        {
            return Connector.WriteValue(this, value);
        }
  
        protected virtual void RaiseOnSetValue(object sender, IRtValue value, DateTime? updated = null, bool status = true)
        {
            Value = value ?? valueNull;
            Updated = updated?? DateTime.Now;
            Status = status;

            ValueSetted?.Invoke(sender, this);
        }


        protected virtual void RaiseOnUpdatedValue(object sender, IRtValue value, DateTime? updated = null, bool status = true)
        {
            Value = value ?? valueNull;
            Updated = updated ?? DateTime.Now;
            Status = status;

            ValueUpdated?.Invoke(sender, this);
        }

        protected virtual void RaiseStatusChanged(object sender, bool status)
        {
            Status = false; 
        }

        protected void DettachEventHandler()
        {
            ValueUpdated = null;
            ValueSetted = null;
        }

        public virtual void Dispose()
        {
            DettachEventHandler();
        }

        public bool Equals(RtTagBase x, RtTagBase y)
        {
            return string.Equals(x.TagName, y.TagName);
        }

        public int GetHashCode([DisallowNull] RtTagBase obj)
        {
            return obj.TagName.GetHashCode();
        }

        public override string ToString()
        {
            return $"{TagName}={Value.Text}"; 
        }
    }
}

