namespace DeltaX.Rpc.JsonRpc
{
    using ImpromptuInterface;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Reflection;
    using System.Threading.Tasks;

    class DynamicRpcCaller<TInterface> : DynamicObject where TInterface : class
    {
        private Dictionary<string, Type> returnTypes = new Dictionary<string, Type>();
        private Rpc rpc;
        private string namePrefix;
        private int timeoutMs;
        private readonly bool isNotification;

        public DynamicRpcCaller(Rpc rpc, string namePrefix = null, int timeoutMs = 10000, bool isNotification = false)
        {
            this.rpc = rpc;
            this.namePrefix = namePrefix ?? "";
            this.timeoutMs = timeoutMs;
            this.isNotification = isNotification;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Type retType;

            // Cache
            if (!returnTypes.TryGetValue(binder.Name, out retType))
            {
                var mtd = typeof(TInterface).GetMethod(binder.Name);
                retType = mtd.ReturnType;
                returnTypes.TryAdd(binder.Name, retType);
            }

            var isGeneric = retType.IsGenericType;
            var isAwaitable = retType.GetMethod(nameof(Task.GetAwaiter)) != null;
            var isVoid = retType == typeof(void);

            // Notifiaction
            if(isNotification || isVoid)
            {
                rpc.NotifyAsync($"{namePrefix}{binder.Name}", args);
                result = default;
                return true;
            }

            // CallResultAsync
            if (isAwaitable && isGeneric)
            {
                retType = retType.GetProperty("Result").PropertyType;
                var mtd = this.GetType().GetMethod(nameof(this.CallResultAsync), BindingFlags.NonPublic | BindingFlags.Instance);
                var mtdGen = mtd.MakeGenericMethod(retType);
                result = mtdGen.Invoke(this, new object[] { binder.Name, args });
                return true;
            }
            // CallVoidAsync
            if (isAwaitable && !isGeneric)
            {
                result = CallAsync(binder.Name, args);
                return true;
            }

            // Call Sync
            var task = rpc.RemoteCallAsync($"{namePrefix}{binder.Name}", args);
            if (task.Wait(timeoutMs))
            {
                result = retType == typeof(void) ? null : task.Result.GetResultType(retType);
                return true;
            }

            result = default;
            return false; 
        }

        private async Task<TResult> CallResultAsync<TResult>(string methodName, object args)
        {
            var taskResp = await rpc.RemoteCallAsync(methodName, args);
            return taskResp.GetResult<TResult>();
        }

        private async Task CallAsync(string methodName, object args)
        {
            await rpc.RemoteCallAsync(methodName, args);
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        { 
            var valid = binder.Type == typeof(TInterface);
            result = Impromptu.ActLike<TInterface>(this);
            return valid;
        }
    }
}
