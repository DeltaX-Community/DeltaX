namespace DeltaX.Connections.HttpServer
{
    using System; 
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class App
    {
        public App(Listener listener, Routes routes = null)
        {
            Listener = listener ?? new Listener();
            Routes = routes ?? new Routes();
        }

        public Listener Listener { get; }
        public Routes Routes { get; }


        public void Run()
        {
            RunAsync().Wait();
        }

        public Task RunAsync()
        {
            Listener.OnRequest += (request, response) => Routes.InvokeHandlerAsync(request, response);
            return Listener.StartAsync();
        }
    }


    public static class AppDemo
    {
        public static void Test1()
        {
            // Listener listener = new Listener(8081, "172.17.36.142");
            Listener listener = new Listener(8080);
            App app = new App(listener);

            DateTime dateStart = DateTime.Now;
            app.Routes.Get("/", async (match, req, res) =>
            {
                await res.SendAsync("<p>Live server since " + (DateTime.Now - dateStart).ToString(@"dd\.hh\:mm\:ss") + " time.</p>", "text/html");
            });

            app.Routes.Get("/json", async (match, req, res) =>
            {
                await res.SendAsync("Ok");
            });

            // Regex rxFile = new Regex(@"/files/((?<dirname>[A-Za-z0-9-]+)/)*(?<filename>[^\s/]*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase); 
            Regex rxFile = new Regex(@"/Downloads/(?<filepath>([^\s/]+/)*[^\s/]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase); 
            app.Routes.Get(rxFile, async (match, req, res) =>
            {
                string filepath = match.Groups["filepath"].Value;
                string p = string.Format("{0}/{1}", @"C:\Users\c.hsiman\Downloads", filepath);
                await res.SendFileAsync(p);
            });

            app.Run();
        }
    }
}
