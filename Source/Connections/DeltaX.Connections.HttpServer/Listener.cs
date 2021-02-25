namespace DeltaX.Connections.HttpServer
{
    using Microsoft.Extensions.Logging;
    using System; 
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class Listener
    {
        private ILogger logger;
        private ManualResetEvent isFinished;

        public delegate Task OnRequestEventHandler(Request request, Response response);

        public event OnRequestEventHandler OnRequest;

        public HttpListener HttpListener { get; private set; }

        public string[] UriPrefixes { get; private set; }


        public Listener(int port = 8080, string interfaceHost = "localhost", ILogger logger = null)
            : this(new string[] { string.Format("http://{1}:{0}/", port, interfaceHost) })
        {
            this.logger = logger;
        }

        public Listener(string[] uriPrefixes, ILogger logger = null)
        {
            UriPrefixes = uriPrefixes ?? throw new ArgumentNullException(nameof(uriPrefixes));
            this.logger = logger;
            HttpListener = new HttpListener();

            foreach (string urip in uriPrefixes)
            {
                HttpListener.Prefixes.Add(urip);
            } 
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
                    Request request = new Request(ctx);
                    Response response = new Response(ctx); 
                    // logger?.LogDebug("{0}: {1} {2}", DateTime.Now, request.Method, request.Endpoint);
                    Console.WriteLine("HttpListener {0}: {1} {2}", DateTime.Now, request.Method, request.Endpoint);

                    OnRequest?.Invoke(request, response);   
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
