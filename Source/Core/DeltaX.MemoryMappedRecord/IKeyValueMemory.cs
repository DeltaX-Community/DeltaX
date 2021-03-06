﻿namespace DeltaX.MemoryMappedRecord
{
    using System;
    using System.Collections.Generic;

    public interface IKeyValueMemory: IDisposable
    {
        int Count { get; } 
        List<string> Keys { get; }
        KeyValueMemoryConfiguration Configuration { get; }

        event EventHandler<List<string>> KeysChanged;

        bool AddValue(string key, byte[] value); 
        void Flush();  
        double GetUpdated(string key); 
        byte[] GetValue(string key); 
        bool SetValue(string key, byte[] value, bool addIfNotExist = false); 
    }
}