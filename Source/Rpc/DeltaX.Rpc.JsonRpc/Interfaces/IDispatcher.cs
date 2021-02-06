namespace DeltaX.Rpc.JsonRpc.Interfaces
{
    using System;
    using System.Collections.Generic;

    public interface IDispatcher
    {
        IEnumerable<string> GetMethods();

        object ExecuteRequest(IMessage message);

        void ExecuteNotification(IMessage message);

        bool RegisterFunction(string functionName, Func<IMessage, object> function);

        bool RegisterFunction(Func<IMessage, object> function);

        bool RegisterFunction(string functionName, Action<IMessage> function);

        bool RegisterFunction(Action<IMessage> function);

        void RegisterService<TSharedInterface>(TSharedInterface instance, string methodsPrefix = null) where TSharedInterface : class;

        void RegisterMethod<T>(T instance, string methodName, string methodsPrefix = null) where T : class;
    }
}

