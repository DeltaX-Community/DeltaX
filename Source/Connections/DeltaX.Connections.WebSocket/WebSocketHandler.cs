namespace DeltaX.Connections.WebSocket
{
    using System;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class WebSocketHandler
    {

        public event EventHandler<bool> OnConnect;
        public event EventHandler<bool> OnClose;
        public event EventHandler<Message> OnMessageReceive;
        public event EventHandler<Message> OnMessageSend;

        WebSocket ws;

        public WebSocketHandler(WebSocket ws)
        {
            this.ws = ws;
        }

        public async Task ReceiveAsync(int bufferSize = 1024 * 4)
        {
            OnConnect?.Invoke(this, ws.State == WebSocketState.Open);
            var buffer = new byte[bufferSize];

            try
            {
                while (true)
                { 
                    var array = new ArraySegment<byte>(buffer); 
                    var result = await ws.ReceiveAsync(array, CancellationToken.None);

                    // FIXME result.CloseStatus or !result.CloseStatus
                    // if (result.MessageType == WebSocketMessageType.Close || !result.CloseStatus.HasValue)
                    if (result.MessageType == WebSocketMessageType.Close || result.CloseStatus.HasValue)
                    {
                        break;
                    }
                    var data = array.Slice(0, result.Count).ToArray();
                    OnMessageReceive?.Invoke(this, new Message { Client = this, MessageType = result.MessageType, Data = data});
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Close();
        }

        public void Close()
        {
            OnClose?.Invoke(this, true);
            ws.Dispose(); 
        }

        public async Task SendAsync(byte[] buffer, WebSocketMessageType messageType = WebSocketMessageType.Text)
        { 
            var t = ws.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), messageType, true, CancellationToken.None);
            await t.ContinueWith((task) =>
            {
                if (task.IsFaulted)
                {
                    Close();
                }
                else
                {
                    OnMessageSend?.Invoke(this, new Message { Client = this, MessageType = messageType, Data = buffer });
                }
            });
        }
    }
}

