namespace DeltaX.RealTime.Decorators
{
    using DeltaX.RealTime.Interfaces;
    using System;

    public abstract class RtTagDecoratorBase : IRtTag, IDisposable
    {
        protected IRtTag tag;
   
        public RtTagDecoratorBase(IRtTag tag)
        {
            this.tag = tag;
            this.tag.ValueSetted += OnTagValueSetted;
            this.tag.ValueUpdated += OnTagValueUpdated;
        }
        
        public virtual event EventHandler<IRtTag> ValueUpdated;
        public virtual event EventHandler<IRtTag> ValueSetted;

        protected void OnTagValueSetted(object sender, IRtTag e)
        {
            ValueSetted?.Invoke(sender, this);
        }

        protected void OnTagValueUpdated(object sender, IRtTag e)
        {
            ValueUpdated?.Invoke(sender, this);
        }

        public virtual IRtConnector Connector
        {
            get { return tag.Connector; }
            protected set { }
        }
   
        public string TagName
        {
            get { return tag.TagName; }
            protected set { }
        }
   
        public virtual string Topic
        {
            get { return tag.Topic; }
            protected set { }
        }
   
        public virtual DateTime Updated
        {
            get { return tag.Updated; }
            protected set { }
        }
   
        public virtual bool Status
        {
            get { return tag.Status; }
            protected set { }
        }
   
        public virtual IRtValue Value
        {
            get { return tag.Value; }
            protected set { }
        }


        // public virtual event EventHandler<IRtTag> OnUpdatedValue
        // {
        //     add { tag.OnUpdatedValue += value; }
        //     remove { tag.OnUpdatedValue -= value; }
        // }
        // 
        // public virtual event EventHandler<IRtTag> OnSetValue
        // {
        //     add { tag.OnSetValue += value; }
        //     remove { tag.OnSetValue -= value; }
        // }
         

        protected virtual void RaiseOnUpdatedValue(object sender, IRtValue value, DateTime? updated = null, bool status = true)
        {
            Value = value;
            Updated = updated ?? DateTime.Now;
            Status = status;

            ValueUpdated?.Invoke(sender, this);
        } 
   
        public virtual bool Set(IRtValue value)
        {
            return tag.Set(value);
        }

        protected void DettachEventHandler()
        {
            ValueUpdated = null;
            ValueSetted = null;
        }

        public virtual void Dispose()
        { 
            tag.ValueSetted -= OnTagValueSetted;
            tag.ValueUpdated -= OnTagValueUpdated;
            DettachEventHandler();
        }
    }

}