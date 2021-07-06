using DeltaX.Modules.RealTimeRpcWebSocket.Configuration; 

public class UIConfiguration : RtWebSocketBridgeConfiguration
{
    public string RealTimeHistoryBasePath { get; set; } 
    public string[] CorsUrls { get; set; } 
}