namespace DeltaX.Connections.HttpServer
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class Routes
    {
        public delegate Task HandlerFunction(Match match, Request request, Response response);

        private Dictionary<HttpMethod, Dictionary<Regex, HandlerFunction>> repository { get; set; }

        public Routes()
        {
            repository = new Dictionary<HttpMethod, Dictionary<Regex, HandlerFunction>>
            {
                {HttpMethod.Get , new Dictionary<Regex, HandlerFunction>() },
                {HttpMethod.Post, new Dictionary<Regex, HandlerFunction>() },
                {HttpMethod.Put, new Dictionary<Regex, HandlerFunction>() },
                {HttpMethod.Delete, new Dictionary<Regex, HandlerFunction>() }
            };
        }

        public void Get(Regex endpointRx, HandlerFunction handler)
        {
            repository[HttpMethod.Get].Add(endpointRx, handler);
        }

        public void Post(Regex endpoint, HandlerFunction handler)
        {
            repository[HttpMethod.Post].Add(endpoint, handler);
        }

        public void Put(Regex endpoint, HandlerFunction handler)
        {
            repository[HttpMethod.Put].Add(endpoint, handler);
        }

        public void Delete(Regex endpoint, HandlerFunction handler)
        {
            repository[HttpMethod.Delete].Add(endpoint, handler);
        }


        public void Get(string endpoint, HandlerFunction handler)
        {
            Get(ConverterRegex(endpoint), handler);
        }

        public void Post(string endpoint, HandlerFunction handler)
        {
            Post(ConverterRegex(endpoint), handler);
        }

        public void Put(string endpoint, HandlerFunction handler)
        {
            Put(ConverterRegex(endpoint), handler);
        }

        public void Delete(string endpoint, HandlerFunction handler)
        {
            Delete(ConverterRegex(endpoint), handler);
        }

        private Regex ConverterRegex(string pattern)
        {
            if (!pattern.StartsWith("^"))
                pattern = "^" + pattern;
            if (!pattern.EndsWith("$"))
                pattern = pattern + "$";

            return new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public async Task InvokeHandlerAsync(Request request, Response response)
        {
            Dictionary<Regex, HandlerFunction> routes;
            Match m;
            HandlerFunction handler;

            if (repository.TryGetValue(request.Method, out routes))
            {
                foreach (var kv in routes)
                {
                    m = kv.Key.Match(request.Endpoint);
                    if (m.Success)
                    {
                        handler = kv.Value;
                        try
                        {
                            await handler.Invoke(m, request, response);
                            await response.CloseAsync();
                        }
                        catch (Exception e)
                        {
                            await response.CloseAsync(HttpStatusCode.InternalServerError, "Internal Error:" + e.Message);
                        }
                        return;
                    }
                }
            }
            Console.WriteLine("{0}: {1} {2} FAIL", DateTime.Now, request.Method, request.Endpoint);
            await response.CloseAsync(HttpStatusCode.NotFound);
        }
    }
}