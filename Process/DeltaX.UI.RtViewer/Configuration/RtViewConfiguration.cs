using DeltaX.Modules.RealTimeRpcWebSocket.Configuration; 

public class RtViewConfiguration : RtWebSocketBridgeConfiguration
{
    public string RealTimeHistoryBasePath { get; set; } 
    public string[] CorsUrls { get; set; } 
}