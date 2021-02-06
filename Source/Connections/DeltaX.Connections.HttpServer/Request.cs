namespace DeltaX.Connections.HttpServer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Request wrapper for HttpListenerRequest
    /// </summary>
    public class Request
    {
        /// <summary>
        /// HttpListenerRequest 
        /// </summary>
        public HttpListenerRequest HttpRequest;
        private string body;

        internal Request(HttpListenerContext context)
        {
            HttpRequest = context.Request;

            Endpoint = HttpRequest.RawUrl.Split('?')[0];

            Parameters = new Dictionary<string, string>();
            foreach (var k in HttpRequest.QueryString.AllKeys)
            {
                if (k != null)
                {
                    Parameters.Add(k, HttpRequest.QueryString.Get(k));
                }
            }
        }

        /// <summary>
        /// Parametros recibidos en request
        /// </summary>
        public Dictionary<string, string> Parameters { get; private set; }

        /// <summary>
        /// Ruta
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// Método invocado
        /// </summary>
        public HttpMethod Method
        {
            get { return new HttpMethod(this.HttpRequest.HttpMethod); }
        }


        /// <summary>
        /// Obtiene la consulta Json como un objeto
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> GetJsonObjectAsync<T>() where T : new()
        {
            if (HttpRequest.ContentType.EndsWith("json", StringComparison.CurrentCultureIgnoreCase))
            {
                var json = await GetBodyAsync();
                return JsonSerializer.Deserialize<T>(json); 
            }

            return default(T);
        }

        /// <summary>
        /// Obtiene la consulta 
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetBodyAsync()
        {
            if (Method == HttpMethod.Get || !this.HttpRequest.HasEntityBody)
                return body;

            if (body == null)
            {
                byte[] buffer = new byte[this.HttpRequest.ContentLength64];
                using (Stream inputStream = this.HttpRequest.InputStream)
                {
                    await inputStream.ReadAsync(buffer, 0, buffer.Length);
                }

                body = Encoding.UTF8.GetString(buffer);
            }

            return body;
        }
    }
}
