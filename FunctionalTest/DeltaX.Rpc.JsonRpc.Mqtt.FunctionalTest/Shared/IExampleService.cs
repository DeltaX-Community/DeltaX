namespace DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Shared
{ 
    using System.Threading.Tasks;

    public interface INotifications
    {
        void OnNotifyBroadcast(object message);
        void OnNotifySum(int result);
    }

    public interface IExampleService
    {
        public int Sum(int a, int b);
        public string Concat(string format, int a, int b);
        public string FuncDemo(int a, string b = "pepe", CustObj obj = default);
        public CustObj FuncCustObj(int a, string b = "pepe", CustObj obj = default);
        Task<float> TaskFloatAsync(int a, int b);
        Task TaskVoidAsync(int a, int b);
        void TaskVoid(int a, int b); 
        Task SendBroadcastAsync(object message);
    }
}
