namespace DeltaX.Rpc.JsonRpc
{
    using DeltaX.CommonExtensions;
    using DeltaX.Rpc.JsonRpc.Interfaces; 
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class RpcDispatcher : IDispatcher
    {
        private class MethodInfoInvoke
        {
            public MethodInfo info;
            public ParameterInfo[] parameters;
            public object instance;
        }

        private ILogger logger;
        private string methodPrefix;
        private Dictionary<string, MethodInfoInvoke> methods = new Dictionary<string, MethodInfoInvoke>();

        public RpcDispatcher(string methodPrefix = null)
        {
            this.methodPrefix = methodPrefix ?? "";
        }
        public IEnumerable<string> GetMethods()
        {
            return methods.Keys.ToArray();
        }

        public virtual void RegisterService<TSharedInterface>(TSharedInterface instance, string methodsPrefix = null)
            where TSharedInterface : class
        {
            var type = typeof(TSharedInterface);
            methodsPrefix ??= "";
            instance = instance ?? throw new ArgumentNullException(nameof(instance));
            var mtds = type.GetMethods();

            foreach (var m in mtds)
            {
                RegisterMethodInfo($"{methodsPrefix}{m.Name}", m, instance);
            }
        }

        public virtual void RegisterMethod<T>(T instance, string methodName, string methodsPrefix = null)
            where T : class
        {
            var type = typeof(T);
            instance = instance ?? throw new ArgumentNullException(nameof(instance));
            methodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            methodsPrefix ??= "";

            var mtd = type.GetMethod(methodName) ?? throw new ArgumentException($"Method {methodName} not found!"); ;

            RegisterMethodInfo($"{methodsPrefix}{mtd.Name}", mtd, instance);
        }


        public virtual bool RegisterMethodInfo(string methodName, MethodInfo method, object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            methodName = $"{methodPrefix}{methodName}";

            logger?.LogDebug("Add method Name:{0}", methodName);
            return methods.TryAdd(methodName, new MethodInfoInvoke()
            {
                info = method,
                instance = instance,
                parameters = method.GetParameters()
            });
        }


        public virtual bool RegisterFunction(string functionName, Func<IMessage, object> function)
        {
            return RegisterMethodInfo(functionName, function.Method, function.Target);
        }

        public virtual bool RegisterFunction(Func<IMessage, object> function)
        {
            return RegisterMethodInfo(function.Method.Name, function.Method, function.Target);
        }

        public virtual bool RegisterFunction(string functionName, Action<IMessage> function)
        {
            return RegisterMethodInfo(functionName, function.Method, function.Target);
        }

        public virtual bool RegisterFunction(Action<IMessage> function)
        {
            return RegisterMethodInfo(function.Method.Name, function.Method, function.Target);
        }

        public virtual void ExecuteNotification(IMessage msg)
        {
            if (methods.ContainsKey(msg.MethodName))
            {
                _ = DoInvoke(msg);
            }
        }

        public virtual object ExecuteRequest(IMessage msg)
        {
            if (methods.ContainsKey(msg.MethodName))
            {
                return DoInvoke(msg).Result;
            }
            else
            {
                logger?.LogError($"Function Request {msg.MethodName} Not Found");
                throw new MissingMethodException($"Function {msg.MethodName} Not Found");
            }
        }

        public virtual async Task<object> DoInvoke(IMessage msg)
        {
            logger?.LogDebug("Execute method Name:{0}", msg.MethodName);
            var msgParam = msg.GetParameters<JsonElement>();

            MethodInfoInvoke method;
            if (methods.TryGetValue(msg.MethodName, out method))
            {
                if (method.parameters.Length == 1 && method.parameters[0].ParameterType == typeof(IMessage))
                {
                    return method.info.Invoke(method.instance, new[] { msg });
                }
                else
                {
                    var args = GetArgument(method.parameters, msgParam);

                    var isGeneric = method.info.ReturnType.IsGenericType;
                    var isAwaitable = method.info.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
                    if (isAwaitable)
                    {
                        if (isGeneric)
                        {
                            dynamic res = await (dynamic)method.info.Invoke(method.instance, args);
                            return res;
                        }
                        await (dynamic)method.info.Invoke(method.instance, args);
                        return null;
                    }
                    else
                    {
                        return method.info.Invoke(method.instance, args);
                    }
                }
            }
            else
            {
                throw new MissingMethodException($"Function {msg.MethodName} Not Found");
            }
        }


        protected virtual object[] GetArgument(ParameterInfo[] parameterInfo, JsonElement json)
        {
            List<object> args = new List<object>();

            bool isArray = json.ValueKind == JsonValueKind.Array;
            bool isObject = json.ValueKind == JsonValueKind.Object;

            foreach (var p in parameterInfo.OrderBy(p => p.Position))
            {
                var parseArg = isArray ? $"{p.Position}" : isObject ? $"{p.Name}" : "";

                var arg = json.JsonGetValue(parseArg, p.ParameterType, p.DefaultValue);
                args.Add(arg);
            }

            return args.ToArray();
        }

        
    }
}