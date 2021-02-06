namespace DeltaX.Rpc.JsonRpc.Mqtt.FunctionalTest.Shared
{ 
    using System.Threading.Tasks;

    public interface INotifications
    {
        void NotificationBroadcast(object message);
        void NotifySumar(int result);
    }

    public interface IExampleService
    {
        public int Sumar(int a, int b);
        public string Concatenar(string format, int a, int b);
        public string FuncDemo(int a, string b = "pepe", CustObj obj = default);
        public CustObj FuncCustObj(int a, string b = "pepe", CustObj obj = default);
        Task<float> TaskIntAsync(int a, int b);
        Task TaskVoidAsync(int a, int b);
        void TaskVoid(int a, int b); 
        Task SendBroadcastAsync(object message);
    }
}
