using System;
using System.Collections.Generic;
using System.Net;

namespace ModbusTcp.Protocol.Reply
{
    class ModbusReadResponseBase : ModbusResponseBase, IModbusReadResponseBase
    {
        public byte Length { get; protected set; }
        public byte[] RawData { get; protected set; }

        public override void FromNetworkBuffer(byte[] buffer)
        {
            var idx = StandardResponseFromNetworkBuffer(buffer);

            Length = buffer[idx++];

            RawData = new byte[Length];
            Buffer.BlockCopy(buffer, idx, RawData, 0, Length);
        }

        public byte[] ReadDataByteSwap()
        {
            var data = new byte[RawData.Length];
            Array.Copy(RawData, data, data.Length);

            for (int idx = 0; idx < data.Length; idx += 2)
            {
                Array.Reverse(data, idx, 2);
            }
            return data;
        } 
    }
}
