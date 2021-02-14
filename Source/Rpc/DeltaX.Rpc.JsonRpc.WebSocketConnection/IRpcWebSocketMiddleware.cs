using DeltaX.Connections.WebSocket;
using DeltaX.Rpc.JsonRpc.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaX.Rpc.JsonRpc.WebSocketConnection
{
    public interface IRpcWebSocketMiddleware
    {
        void Notify(IMessage message);
        IMessage ProcessMessage(WebSocketHandler ws, IMessage msg);

        public Task RunAsync(CancellationToken? cancellationToken = null);
    }
}