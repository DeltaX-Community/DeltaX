namespace DeltaX.Rpc.JsonRpc
{
    using DeltaX.Rpc.JsonRpc.Exceptions;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Security.Principal;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;

    // TODO Utilizar datos de contexto
    public class JsonRpcContext
    { 
        public String  Identity { get; set; }  
    }

    class JsonRpcResponseError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }
    }

    #region Serialize helper
    class JsonRpcRequest<TParam>
    {
        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; set; }
        
        [JsonPropertyName("context")]
        public JsonRpcContext Context { get; set; }

        [JsonPropertyName("params")]
        public TParam Parameters { get; set; }

        [JsonPropertyName("id")]
        public object Id { get; set; }
    }

    class JsonRpcNotification<TParam>
    {
        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("context")]
        public JsonRpcContext Context { get; set; }

        [JsonPropertyName("params")]
        public TParam Parameters { get; set; }
    }

    class JsonRpcResponse<TResult>
    {
        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public object Id { get; set; }

        [JsonPropertyName("result")]
        public TResult Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcResponseError Error { get; set; }
    }

    #endregion


    public class Message : IMessage
    {
        public Message()
        {
            Context = new JsonRpcContext() { Identity = Thread.CurrentPrincipal?.Identity?.Name };
        }

        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("method")]
        public string MethodName { get; set; }

        [JsonPropertyName("context")]
        public JsonRpcContext Context { get; set; }

        [JsonPropertyName("result")]
        public JsonElement Result { set => result = value; }

        [JsonPropertyName("params")]
        public JsonElement Parameters {  set => parameters = value; }

        // TODO: Es necesario hacer esto? 
        // [JsonPropertyName("error")]
        // public JsonRpcResponseError Error { get; set; }

        private object parameters;
        private JsonRpcResponseError error;
        private object result; 
        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions() { IgnoreNullValues = true, WriteIndented = true };
        private IMessage request;
        private string rawDataStr;
        private byte[] rawData;
        private static int msgCount = 1;

        private static string GenerateId(string clientId)
        {
            return $"{clientId}:{msgCount++}";
        }

        public static IMessage CreateRequest<T>(string clientId, string methodName, T arguments)
        {
            return new Message() { Id = GenerateId(clientId), MethodName = methodName, parameters = arguments };
        }

        public static IMessage CreateNotification<T>(string methodName, T arguments)
        {
            return new Message() { Id = null, MethodName = methodName, parameters = arguments };
        }

        public static IMessage CreateResponse<T>(IMessage request, T result)
        {
            return new Message() { Id = request.Id, result = result, request = request };
        }

        public static IMessage CreateResponseError(IMessage request, RpcException exception)
        {
            var error = new JsonRpcResponseError() { Code = exception.ErrorCode ?? -1, Message = exception.Message, Data = exception.StackTrace };
            return new Message() { Id = request.Id, error = error, request = request };
        }
         

        public static IMessage Parse(byte[] rawData)
        {
            var res = JsonSerializer.Deserialize<Message>(rawData);
            res.rawData = rawData;
            return res;
        }

        public static IMessage Parse(string rawData)
        {
            var res = JsonSerializer.Deserialize<Message>(rawData);
            res.rawDataStr = rawData;
            return res;
        }

        public string Serialize()
        {
            if (IsRequest())
            {
                var msg = new JsonRpcRequest<object> { Jsonrpc = Jsonrpc, Method = MethodName, Parameters = parameters, Id = Id, Context = Context };
                return JsonSerializer.Serialize(msg);
            }
            else if (IsNotification())
            {
                var msg = new JsonRpcNotification<object> { Jsonrpc = Jsonrpc, Method = MethodName, Parameters = parameters, Context = Context };
                return JsonSerializer.Serialize(msg);
            }
            else if (IsResponse())
            {
                var msg = new JsonRpcResponse<object> { Jsonrpc = Jsonrpc, Id = Id, Error = error, Result = result };
                return JsonSerializer.Serialize(msg);
            }

            return JsonSerializer.Serialize(this, jsonOptions);
        }

        public byte[] SerializeToBytes()
        {
            if (IsRequest())
            {
                var msg = new JsonRpcRequest<object> { Jsonrpc = Jsonrpc, Method = MethodName, Parameters = parameters, Id = Id };
                return JsonSerializer.SerializeToUtf8Bytes(msg);
            }
            else if (IsNotification())
            {
                var msg = new JsonRpcNotification<object> { Jsonrpc = Jsonrpc, Method = MethodName, Parameters = parameters };
                return JsonSerializer.SerializeToUtf8Bytes(msg);
            }
            else if (IsResponse())
            {
                var msg = new JsonRpcResponse<object> { Jsonrpc = Jsonrpc, Id = Id, Error = error, Result = result };
                return JsonSerializer.SerializeToUtf8Bytes(msg);
            }

            return JsonSerializer.SerializeToUtf8Bytes(this, jsonOptions);
        }

        protected virtual bool ContainId()
        {
            string rid = Id?.ToString();

            return !string.IsNullOrEmpty(rid) && rid.Length > 2;
        }

        public bool IsRequest()
        {
            return !string.IsNullOrEmpty(MethodName) && ContainId();
        }

        public bool IsResponse()
        {
            return string.IsNullOrEmpty(MethodName) && ContainId();
        }

        public bool IsNotification()
        { 
            return !string.IsNullOrEmpty(MethodName) && !ContainId();
        }

        public IMessage GetRequestMessage()
        {
            return request;
        }

        protected T Deserialize<T>()
        {
            if (rawData != null && rawData.Length > 0)
                return JsonSerializer.Deserialize<T>(rawData);

            if (!string.IsNullOrEmpty(rawDataStr))
                return JsonSerializer.Deserialize<T>(rawDataStr);

            return default;
        }

        public T GetParameters<T>()
        {
            if (parameters != null && parameters is T)
            {
                return (T)parameters;
            }

            var dser = Deserialize<JsonRpcRequest<T>>();
            return dser != null ? dser.Parameters : default;
        }

        public T GetResult<T>()
        {
            if (result != null && result is T)
            {
                return (T)result;
            }

            var dser = Deserialize<JsonRpcResponse<T>>(); 
            if (dser?.Error != null)
            {
                throw new RpcException(dser.Error.Code, dser.Error.Message, dser.Error.Data?.ToString());
            }

            return dser != null ? dser.Result : default;
        }

        public object GetResultType(Type type)
        {
            var mtd = GetType().GetMethod(nameof(GetResult));
            var mtdGen = mtd.MakeGenericMethod(type);
            return mtdGen.Invoke(this, null);
        }
    }
}
