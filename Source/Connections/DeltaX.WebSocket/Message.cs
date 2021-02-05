namespace DeltaX.Connections.WebSocket
{
    using System.Net.WebSockets;

    public class Message
    {
        public WebSocketHandler Client { get; internal set; }
        public WebSocketMessageType MessageType { get; internal set; }
        public byte[] Data { get; internal set; }
    }
}

