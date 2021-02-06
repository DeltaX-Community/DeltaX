namespace DeltaX.Connections.WebSocket
{
    using System.Net.WebSockets; 
    using System.Collections.Concurrent; 
    using System.Threading.Tasks;

    public class WebSocketServer
    {
        ConcurrentDictionary<WebSocketHandler, Task> receiveTasks;

        public WebSocketServer(WebSocketListener listener = null, WebSocketHandlerHub hub = null)
        {
            this.receiveTasks = new ConcurrentDictionary<WebSocketHandler, Task>();
            this.Listener = listener ?? new WebSocketListener();
            this.Hub = hub ?? new WebSocketHandlerHub();

            this.Listener.OnConnect += Listener_OnConnect;
            this.Hub.OnClose += Hub_OnClose;
        }

        private void Hub_OnClose(object sender, WebSocketHandler ws)
        {
            receiveTasks.TryRemove(ws, out _);
        }

        public WebSocketHandlerHub Hub { get; set; }

        public WebSocketListener Listener { get; set; }

        private void Listener_OnConnect(object sender, WebSocket e)
        {
            var ws = Hub.RegisterWebSocket(e);
            var task = ws.ReceiveAsync();
            receiveTasks.TryAdd(ws, task);
        }
    }
}
