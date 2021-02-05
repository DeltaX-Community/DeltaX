namespace DeltaX.Rpc.JsonRpc
{
    using DeltaX.Rpc.JsonRpc.Exceptions;
    using DeltaX.Rpc.JsonRpc.Interfaces;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;


    public class Rpc
    { 
        private IRpcConnection connection;
        private ILogger logger;
        private Dictionary<string, dynamic> callers = new Dictionary<string, dynamic>();

        public Rpc(IRpcConnection connection, IDispatcher dispatcher = null, ILogger logger = null, int timeoutMs = 30000 )
        {
            this.connection = connection;
            this.Dispatcher = dispatcher ?? new RpcDispatcher();
            this.logger = logger;
            TimeoutMs = timeoutMs;

            this.connection.OnReceive += OnConnectionReceive;
        }

        public IDispatcher Dispatcher { get; private set; }

        public int TimeoutMs { get; set; }

        protected virtual void OnConnectionReceive(object sender, IMessage msg)
        {
            Task.Run(() => ExecuteMethod(msg));
        }

        protected virtual void ExecuteMethod(IMessage message)
        {
            try
            {
                if (message.IsRequest())
                {
                    try
                    {
                        var response = Dispatcher.ExecuteRequest(message);
                        if (response is IMessage msgResp)
                        {
                            if (!msgResp.IsResponse())
                            {
                                throw new Exception("Bad internal response on invoke " + message.MethodName);
                            }
                            connection.SendResponseAsync(msgResp);
                        }
                        else
                        {
                            connection.SendResponseAsync(Message.CreateResponse(message, response));
                        }
                    }
                    catch (RpcException e)
                    {
                        var response = Message.CreateResponseError(message, e);
                        connection.SendResponseAsync(response);
                    }
                    catch (Exception e)
                    {
                        // TODO Internal server error
                        var response = Message.CreateResponseError(message, new RpcException(e.InnerException ?? e));
                        connection.SendResponseAsync(response);
                    }
                }
                else if (message.IsNotification())
                {
                    Dispatcher.ExecuteNotification(message);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("ExecuteMethod Exception {0} ", ex);
            }
        }

        public void UpdateRegisteredMethods()
        {
            connection.UpdateRegisteredMethods(Dispatcher.GetMethods());
        } 
            
        /// <summary>
        /// Publish Notification Message
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="arguments"></param>
        public virtual Task RemoteNotificationAsync(string methodName, object arguments)
        {
            var msg = Message.CreateNotification(methodName, arguments);
            return connection.SendNotificationAsync(msg);
        }


        /// <summary>
        /// Publish a request message and wait response async
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="arguments"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public virtual Task<IMessage> RemoteCallAsync(string methodName, object arguments)
        {
            return Task.Run(() =>
            {
                var msgReq = Message.CreateRequest(connection.ClientId, methodName, arguments);
                var task = connection.SendRequestAsync(msgReq);

                if (task.Wait(TimeoutMs))
                {
                    return task;
                }
                throw new TimeoutException();
            });
        }

        public virtual Task CallAsync(string methodName, params object[] args)
        {
            var taskResp = RemoteCallAsync(methodName, args);
            return taskResp;
        }

        public virtual Task<TResult> CallAsync<TResult>(string methodName, params object[] args)
        {
            var taskResp = RemoteCallAsync(methodName, args);
            return taskResp.ContinueWith(t => t.Result.GetResult<TResult>());
        }
         
        public virtual TResult Call<TResult>(string methodName, params object[] args)
        {
            var task = RemoteCallAsync(methodName, args);
            var msgResp = task.Result;
            return msgResp.GetResult<TResult>();
        }
         
        public virtual Task NotifyAsync(string methodName, params object[] args)
        {
            return RemoteNotificationAsync(methodName, args);
        }
         
        public virtual void Notify(string methodName, params object[] args)
        {
            RemoteNotificationAsync(methodName, args).Wait();
        }

        /// <summary>
        /// Get Services interface for call methos defined inner dynamically 
        /// using request
        /// - MethodName: {namePrefix}{method.Name}
        /// - Arguments: Array of arguments used on caller 
        /// - Response: typeof method response 
        /// </summary>
        /// <typeparam name="TSharedInterface">The interface implemented on Rpc server</typeparam>
        /// <param name="namePrefix">Prefix for message</param>
        /// <returns></returns>
        public TSharedInterface GetServices<TSharedInterface>(string namePrefix = null, int timeoutMs = 10000) where TSharedInterface : class
        {
            dynamic caller;
            var serviceInterface = typeof(TSharedInterface);
            string callerName = $"{namePrefix ?? ""}{serviceInterface.FullName}";

            if (!serviceInterface.IsInterface)
            {
                throw new Exception($"GetServices <{serviceInterface.FullName}> is not Interface");
            }

            // Add to stack if not exist
            if (!callers.TryGetValue(callerName, out caller))
            {
                caller = new DynamicRpcCaller<TSharedInterface>(this, namePrefix, timeoutMs);
                callers.Add(callerName, caller);
            }

            // Implicit convertion
            return caller;
        }
    }
}

