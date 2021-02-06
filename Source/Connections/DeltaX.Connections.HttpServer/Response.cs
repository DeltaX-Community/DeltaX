namespace DeltaX.Connections.HttpServer
{
    using System; 
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class Response
    {
        /// <summary>
        /// HttpListenerResponse
        /// </summary>
        public HttpListenerResponse HttpResponse { get; private set; }

        /// <summary>
        /// Contendio a responder
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Tipo de contenido a responder ej: text/html
        /// </summary>
        public string ContentType { get; set; }

        internal Response(HttpListenerContext context)
        {
            HttpResponse = context.Response;
            ContentType = context.Request.ContentType ?? HttpResponse.ContentType;
        }

        public Task SendJsonAsync<T>(T obj, HttpStatusCode status = HttpStatusCode.OK)
        {  
            string json = JsonSerializer.Serialize(obj);
            return SendAsync(json, "application/json", status);
        }


        public async Task SendFileAsync(string filepath)
        {
            try
            {
                bool error = false;
                Stream reader = null;
                Stream output = null;

                if (!File.Exists(filepath))
                {
                    await CloseAsync(HttpStatusCode.NotFound, "File not Found");
                    return;
                }

                try
                {
                    output = HttpResponse.OutputStream;
                    reader = File.Open(filepath, FileMode.Open, FileAccess.Read);

                    var f = new FileInfo(filepath);
                    HttpResponse.Headers.Add("Content-Disposition", "attachment;filename=" + f.Name);
                    HttpResponse.Headers.Add("Date", DateTime.Now.ToString("r"));
                    HttpResponse.Headers.Add("Last-Modified", f.LastWriteTime.ToString("r"));
                    HttpResponse.ContentLength64 = reader.Length;
                    // HttpResponse.ContentType = MimeMapping.GetMimeMapping(filepath);

                    // await reader.CopyToAsync(output);

                    /// No uso reader.CopyToAsync(output); porque no logro trapear el error cuando el usuario cancela
                    /// y el archivo me queda bloqueado
                    long offset = 0;
                    byte[] buff = new byte[10240];
                    do
                    {
                        int len = (int)(buff.Length > reader.Length - offset ? reader.Length - offset : buff.Length);
                        len = await reader.ReadAsync(buff, 0, len);
                        try
                        {
                            await output.WriteAsync(buff, 0, len);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("CopyToAsync WriteAsync:" + e.Message);
                            throw e;
                        }
                        offset += len;
                    } while (offset < reader.Length);
                }
                catch (Exception e)
                {
                    error = true;
                    Console.WriteLine("CopyToAsync E:" + e.Message);
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }

                    if (error)
                    {
                        await CloseAsync(HttpStatusCode.InternalServerError, "Error al leer el archivo");
                    }

                    if (output != null)
                    {
                        output.Close();
                        output.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await CloseAsync(HttpStatusCode.InternalServerError);
            }
        }

        public async Task CloseAsync(HttpStatusCode ? status = null, string message = null)
        {
            if (status.HasValue)
            {
                HttpResponse.StatusCode = (int)status.Value;
            }

            if (!string.IsNullOrEmpty(message))
            {
                byte[] responseBuffer = Encoding.UTF8.GetBytes(message);
                await this.HttpResponse.OutputStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
            }

            HttpResponse.Close();
        }

        public async Task SendAsync(string content = null, string contentType = null, HttpStatusCode status = HttpStatusCode.OK)
        {
            content = content ?? Content ?? throw new ArgumentNullException(nameof(Content));
            contentType = contentType ?? ContentType ?? throw new ArgumentNullException(nameof(ContentType));

            byte[] responseBuffer = Encoding.UTF8.GetBytes(content);
            HttpResponse.ContentType = contentType;
            HttpResponse.StatusCode = (int)status;

            if (HttpResponse.ContentLength64 == 0)
                HttpResponse.ContentLength64 = responseBuffer.Length;

            using (Stream output = HttpResponse.OutputStream)
            {
                await output.WriteAsync(responseBuffer, 0, responseBuffer.Length);
            }
            HttpResponse.Close();
        }
    }
}
