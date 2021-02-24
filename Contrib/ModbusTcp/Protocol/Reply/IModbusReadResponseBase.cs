namespace ModbusTcp.Protocol.Reply
{
    public interface IModbusReadResponseBase
    { 
        byte[] RawData { get; }
        byte Length { get; }
        byte[] ReadDataByteSwap(); 
    }
}