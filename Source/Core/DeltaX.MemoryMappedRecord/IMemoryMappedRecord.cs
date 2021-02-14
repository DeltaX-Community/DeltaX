namespace DeltaX.MemoryMappedRecord
{
    using System;
    using System.Collections.Generic;

    public interface IMemoryMappedRecord: IDisposable
    {
        int BlockSize { get; set; }
        int BlockSizeMin { get; set; }
        long HeaderCapacity { get; }
        int HeaderCount { get; }
        int HeaderNextPosition { get; }
        int HeaderSize { get; }
        int RecordDataOffset { get; }
         
        void CleanValue(int position);
        void Flush();
        double ReadDouble(int position);
        short ReadInt16(int position);
        int ReadInt32(int position);
        byte[] ReadRecord(int position);
        List<int> ReadRecordsPositions(int initialPosition = 512);
        void Write(int position, double value);
        void Write(int position, int value);
        void Write(int position, short value);
        int WriteRecord(int position, byte[] value, int length = -1); 
        int AddRecord(byte[] value, int length = -1);
    }
}