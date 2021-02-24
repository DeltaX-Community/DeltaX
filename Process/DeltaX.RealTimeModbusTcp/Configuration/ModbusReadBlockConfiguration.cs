public class ModbusReadBlockConfiguration
{
    public ModbusFunctions Function { get; set; }
    public int AddressOffset { get; set; }
    public int Count { get; set; }
    public bool ByteSwap { get; set; } 
    public ModbusRtTagConfiguration[] Tags { get; set; }
}
