namespace DeltaX.RealTime.Decorators
{
    using DeltaX.RealTime.Interfaces;
    using System;

    public abstract class RtTagDecoratorBase : IRtTag, IDisposable
    {
        protected IRtTag tag;
        protected static IRtValue valueNull = RtValue.Create(string.Empty);
        public RtTagDecoratorBase(IRtTag tag)
        {
            this.tag = tag;
            this.tag.ValueSetted += OnTagValueSetted;
            this.tag.ValueUpdated += OnTagValueUpdated;
            this.tag.StatusChanged += OnStatusChanged;
        }
        
        public virtual event EventHandler<IRtTag> ValueUpdated;
        public virtual event EventHandler<IRtTag> ValueSetted;
        public virtual event EventHandler<IRtTag> StatusChanged;

        protected void OnTagValueSetted(object sender, IRtTag e)
        {
            ValueSetted?.Invoke(sender, this);
        }

        protected void OnTagValueUpdated(object sender, IRtTag e)
        {
            ValueUpdated?.Invoke(sender, this);
        }

        protected void OnStatusChanged(object sender, IRtTag e)
        {
            StatusChanged?.Invoke(sender, this);
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
        }
        
        public virtual IRtTagOptions Options
        {
            get { return tag.Options; } 
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
            tag.StatusChanged -= OnStatusChanged;
            DettachEventHandler();
        }
        public override string ToString()
        {
            return $"{TagName}={Value.Text}";
        }
    }

}