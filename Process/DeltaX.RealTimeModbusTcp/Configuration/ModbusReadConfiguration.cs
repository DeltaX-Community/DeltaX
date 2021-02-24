public class ModbusReadConfiguration
{
    public string ProcessInfoName { get; set; } 
    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 502;
    public int TimeoutSeconds { get; set; } = 10;
    public int ScanIntervalMilliSeconds { get; set; } = 500;
    public ModbusReadBlockConfiguration[] ReadBlocks { get; set; } 
} 