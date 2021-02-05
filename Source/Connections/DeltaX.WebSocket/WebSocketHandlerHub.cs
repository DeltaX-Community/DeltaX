namespace DeltaX.Connections.WebSocket
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Net.WebSockets;
    using System.Linq;
    using System.Threading.Tasks;

    public class WebSocketHandlerHub
    { 
        public event EventHandler<WebSocketHandler> OnClose;
        public event EventHandler<WebSocketHandler> OnConnect;
        public event EventHandler<Message> OnMessageReceive;
        public event EventHandler<Message> OnMessageSend;
         
        HashSet<WebSocketHandler> Clients; 

        public WebSocketHandlerHub()
        {
            Clients = new HashSet<WebSocketHandler>();
        }


        public WebSocketHandler RegisterWebSocket(WebSocket ws)
        {
            var client = new WebSocketHandler(ws);
            Clients.Add(client); 

            client.OnClose += WshOnClose;
            client.OnConnect += WshOnConnect;
            client.OnMessageReceive += WshOnMessageReceive;
            client.OnMessageSend += WshOnMessageSend; 

            return client;
        }

        virtual protected void WshOnMessageSend(object sender, Message message)
        { 
            OnMessageSend?.Invoke(this, message);
        }

        virtual protected void WshOnMessageReceive(object sender, Message message)
        { 
            OnMessageReceive?.Invoke(this, message);
        }

        virtual protected void WshOnClose(object sender, bool e)
        {
            var client = sender as WebSocketHandler;
            OnClose?.Invoke(this, client);
            Clients.Remove(client);
        }

        virtual protected void WshOnConnect(object sender, bool e)
        {
            var client = sender as WebSocketHandler;
            OnConnect?.Invoke(this, client);
        }

        public void CloseAll()
        {
            foreach (var client in Clients.ToArray())
            {
                client.Close();
            }
            Clients.Clear();
        }

        public async Task SendAllAsync(byte[] buffer, WebSocketMessageType messageType = WebSocketMessageType.Text)
        {
            foreach (var client in Clients.ToArray())
            {
                client.SendAsync(buffer);
            } 
        }
    }
}

