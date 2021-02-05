namespace DeltaX.Rpc.JsonRpc.Exceptions
{
    using System;

    public class RpcException : Exception
    {
        protected string stackTrace;

        public RpcException(Exception ex) : this(0, ex.Message, ex.StackTrace) { }

        public RpcException(int errorCode, string Message, string stackTrace) : base(Message)
        {
            ErrorCode = errorCode;
            this.stackTrace = stackTrace;
        }

        public override string StackTrace => stackTrace;

        public int? ErrorCode { get; private set; }
    }
}
