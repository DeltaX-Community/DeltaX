namespace DeltaX.RealTime.Interfaces
{
    using System;
     
    public interface IRtMessage
    {
        string Topic { get; }
              
        IRtValue Value { get; }

        object Raw { get; }
    }
}
