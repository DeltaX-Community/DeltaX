namespace DeltaX.RealTime
{
    using DeltaX.CommonExtensions;
    using DeltaX.RealTime.Interfaces;
    using System;
    using System.Text.Json;

    public abstract class RtTagBase : IRtTag, IDisposable
    { 
        public virtual event EventHandler<IRtTag> ValueUpdated;
        public virtual event EventHandler<IRtTag> ValueSetted;
           
        public virtual IRtConnector Connector { get; protected set; }

        public virtual string TagName { get; protected set; }

        public virtual string Topic { get; protected set; }         

        public virtual IRtValue Value { get; protected set; } = RtValue.Create(string.Empty);

        public virtual DateTime Updated { get; protected set; } = DateTime.Now;
         
        protected bool _status;
        public virtual bool Status
        {
            get
            {
                if (!Connector.IsConnected && _status)
                    _status = false;
                return _status;
            }
            protected set
            {
                _status = value;
            }
        }

        public virtual bool Set(IRtValue value)
        {
            return Connector.WriteValue(this, value);
        }
  
        protected virtual void RaiseOnSetValue(object sender, IRtValue value, DateTime? updated = null, bool status = true)
        {
            Value = value;
            Updated = updated?? DateTime.Now;
            Status = status;

            ValueSetted?.Invoke(sender, this);
        }


        protected virtual void RaiseOnUpdatedValue(object sender, IRtValue value, DateTime? updated = null, bool status = true)
        {
            Value = value;
            Updated = updated ?? DateTime.Now;
            Status = status;

            ValueUpdated?.Invoke(sender, this);
        }

        // protected virtual void RaiseOnUpdatedValueJson(object sender, JsonElement value, DateTime? updated = null, bool status = true)
        // {
        //     if (TagType != RtTagType.Json)
        //     {
        //         throw new Exception($"Tag {TagName} isnt Json Type");
        //     }
        // 
        //     var obj = value.JsonGetValue(TagTypeParser); 
        //     var parsed = Convert.ToString(obj);
        //     RaiseOnUpdatedValue(sender, RtValue.Create(parsed), updated, status);
        // }
        // 
        // 
        // 
        // protected virtual void RaiseOnUpdatedValueUltraLight(object sender, string value, DateTime? updated = null, bool status = true)
        // {
        //     string _ul_field;
        //     string _ul_command;
        //     string _ul_device;
        // 
        //     if (TagType != RtTagType.UltraLight)
        //     {
        //         throw new Exception($"Tag {TagName} isnt UltraLight Type");
        //     }
        //     if (string.IsNullOrEmpty(_ul_field))
        //     {
        //         TagTypeParser.TryUltraLightParse(out _ul_device, out _ul_command, out _ul_field);
        //     }
        // 
        //     var parsed = value.UltraLightGetValue(_ul_field, _ul_command, _ul_device);
        //     if (!string.IsNullOrEmpty(parsed))
        //     { 
        //         RaiseOnUpdatedValue(sender, RtValue.Create(parsed), updated, status);
        //     }
        // }


       //  protected virtual void RaiseOnDisconnect(object sender, bool status)
       //  {
       //      Status = status;
       //  }


        protected void DettachEventHandler()
        {
            ValueUpdated = null;
            ValueSetted = null;
        }

        public virtual void Dispose()
        {
            DettachEventHandler();
        }
    }
}

