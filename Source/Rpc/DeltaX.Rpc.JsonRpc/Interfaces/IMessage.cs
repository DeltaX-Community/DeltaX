namespace DeltaX.Rpc.JsonRpc.Interfaces
{
    using System;

    public interface IMessage
    {  
        string Id { get; set; }

        string MethodName { get; }

        bool IsRequest();

        bool IsResponse();

        bool IsNotification();

        IMessage GetRequestMessage();

        T GetParameters<T>();

        T GetResult<T>();

        object GetResultType(Type type);

        string Serialize();

        byte[] SerializeToBytes();
    }
}