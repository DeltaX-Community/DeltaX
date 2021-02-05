namespace DeltaX.Rpc.JsonRpc.Exceptions
{
    public class RpcExceptionMethodNotFound : RpcException
    {
        public RpcExceptionMethodNotFound() : base(-32601, "Method not found", null) { }
    }
}
