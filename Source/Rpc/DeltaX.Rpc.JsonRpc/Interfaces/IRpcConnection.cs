﻿namespace DeltaX.Rpc.JsonRpc.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRpcConnection
    {
        string ClientId { get; }

        event EventHandler<IMessage> OnReceive;
        
        event EventHandler<bool> OnConnectionChange;

        public Task RunAsync(CancellationToken? cancellationToken = null);

        bool IsConnected();

        bool UpdateRegisteredMethods(IEnumerable<string> methods);

        Task SendResponseAsync(IMessage message);
        
        Task SendNotificationAsync(IMessage message);

        Task<IMessage> SendRequestAsync(IMessage message);
    }
}
