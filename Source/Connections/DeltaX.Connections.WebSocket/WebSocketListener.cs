namespace DeltaX.Connections.WebSocket
{
    using System.Net.WebSockets;
    using Microsoft.Extensions.Logging;
    using System; 
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks; 
    using DeltaX.Configuration;

    public class WebSocketListener
    {
        private readonly ILogger logger;
        private ManualResetEvent isFinished;

        public event EventHandler<WebSocket> OnConnect;

        public HttpListener HttpListener { get; private set; }

        public string[] UriPrefixes { get; private set; }


        public WebSocketListener(int port = 8080, string interfaceHost = "localhost", ILogger logger = null)
            : this(new string[] { string.Format("ws://{1}:{0}/", port, interfaceHost) }, logger)
        {
        }

        public WebSocketListener(string[] uriPrefixes, ILogger logger = null)
        {
            this.UriPrefixes = uriPrefixes ?? throw new ArgumentNullException(nameof(uriPrefixes));
            this.HttpListener = new HttpListener();
            this.logger = logger ?? Configuration.DefaultLogger;

            foreach (string urip in uriPrefixes)
            {
                HttpListener.Prefixes.Add(urip);
            }
        }

        public WebSocketListener(HttpListener httpListener, ILogger logger= null)
        {
            this.HttpListener = httpListener ?? throw new ArgumentNullException(nameof(httpListener));
            this.logger = logger ?? Configuration.DefaultLogger;
        }


        public async Task StartAsync()
        {
            try
            {
                HttpListener.Start();
                isFinished = new ManualResetEvent(false);
                logger?.LogInformation("HttpListener for requests on [{0}] running!", string.Join(", ", UriPrefixes));

                while (!isFinished.WaitOne(0) && HttpListener.IsListening)
                {
                    var ctx = await HttpListener.GetContextAsync();
                    if (ctx.Request.IsWebSocketRequest)
                    {
                        var ws = await ctx.AcceptWebSocketAsync(string.Empty);
                        OnConnect?.Invoke(this, ws.WebSocket);
                    }
                    else
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        ctx.Response.Close();
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogError("HttpListener StartAsync Error:{0}", e.Message);
            }
            finally
            {
                HttpListener = null;
            }

            logger?.LogInformation("HttpListener for requests on {0} FINISHED", string.Join(", ", UriPrefixes));
        }

        public void Stop()
        {
            logger?.LogInformation("HttpListener Stop()");
            isFinished.Set();
            HttpListener?.Stop();
        }
    }
}

