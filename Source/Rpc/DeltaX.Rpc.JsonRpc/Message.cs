namespace DeltaX.Rpc.JsonRpc
{
    using DeltaX.Rpc.JsonRpc.Exceptions;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using System;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Serialization;


    #region Serialize helper

    class JsonRpcResponseError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }
    }

    class JsonRpcParameters<TParam>
    {
        [JsonPropertyName("params")]
        public TParam Parameters { get; set; }
    }

    class JsonRpcResult<TResult>
    {
        [JsonPropertyName("result")]
        public TResult Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcResponseError Error { get; set; }
    }
    #endregion


    public class Message : IMessage
    {
        [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public object Id { get; set; }
        public string IdString { get; set; }

        [JsonPropertyName("method")]
        public string MethodName { get; set; }

        [JsonPropertyName("result")]
        public JsonElement Result { set => result = value; }

        [JsonPropertyName("params")]
        public JsonElement Parameters { set => parameters = value; }

        private object parameters;
        private JsonRpcResponseError error;
        private object result;
        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions() { IgnoreNullValues = true, WriteIndented = true };
        private IMessage request;
        private string rawDataStr;
        private byte[] rawData;
        private static int msgCount = 1;
        private MethodInfo methodGetResult;

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
                var msg = new { jsonrpc = Jsonrpc, method = MethodName, @params = parameters, id = Id };
                return JsonSerializer.Serialize(msg);
            }
            else if (IsNotification())
            {
                var msg = new { jsonrpc = Jsonrpc, method = MethodName, @params = parameters };
                return JsonSerializer.Serialize(msg);
            }
            else if (IsResponse())
            {
                if (error == null)
                {
                    return JsonSerializer.Serialize(new { jsonrpc = Jsonrpc, id = Id, result = result });
                }
                else
                {
                    return JsonSerializer.Serialize(new { jsonrpc = Jsonrpc, id = Id, error = error });
                }
            }

            return JsonSerializer.Serialize(this, jsonOptions);
        }

        public byte[] SerializeToBytes()
        {
            if (IsRequest())
            {
                var msg = new { jsonrpc = Jsonrpc, method = MethodName, @params = parameters, id = Id };
                return JsonSerializer.SerializeToUtf8Bytes(msg);
            }
            else if (IsNotification())
            {
                var msg = new { jsonrpc = Jsonrpc, method = MethodName, @params = parameters };
                return JsonSerializer.SerializeToUtf8Bytes(msg);
            }
            else if (IsResponse())
            {
                if (error == null)
                {
                    return JsonSerializer.SerializeToUtf8Bytes(new { jsonrpc = Jsonrpc, id = Id, result = result });
                }
                else
                {
                    return JsonSerializer.SerializeToUtf8Bytes(new { jsonrpc = Jsonrpc, id = Id, error = error });
                }
            }

            return JsonSerializer.SerializeToUtf8Bytes(this, jsonOptions);
        }

        protected virtual bool ContainId()
        {
            string rid = Id?.ToString();

            return !string.IsNullOrEmpty(rid);
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

            var dser = Deserialize<JsonRpcParameters<T>>();
            return dser != null ? dser.Parameters : default;
        }

        public T GetResult<T>()
        {
            if (result != null && result is T)
            {
                return (T)result;
            }

            var dser = Deserialize<JsonRpcResult<T>>();
            if (dser?.Error != null)
            {
                throw new RpcException(dser.Error.Code, dser.Error.Message, dser.Error.Data?.ToString());
            }

            result = dser != null ? dser.Result : default(T);
            return (T)result;
        }

        public object GetResultType(Type type)
        {
            methodGetResult ??= typeof(Message).GetMethod(nameof(GetResult));
            var mtdGen = methodGetResult.MakeGenericMethod(type);
            return mtdGen.Invoke(this, null);
        }
    }
}
