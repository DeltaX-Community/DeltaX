namespace DeltaX.MemoryMappedRecord
{
    using DeltaX.CommonExtensions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// 
    /// Memoria de indices: El Data (bytes[]) de la memoria de indices está formado por:
    ///     | dataIdx (int)  | UpdateTime (double) |  Key (bytes[N]) |
    ///     
    /// Memoria de datos: El Data (bytes[]) de la memoria de datos esta formado por:
    ///     | raw data on positon (dataIdx) |
    /// y la referencia de dataIndex, que es la posición de data en se guarda en la memoria de index
    /// 
    /// </summary>
    public class KeyValueMemory : IKeyValueMemory
    {
        private MemoryMappedRecord mmIndex;
        private MemoryMappedRecord mmData;
        private int nextPosition;
        private const int OffsetDataIndex = 0;
        private const int OffsetUpdate = sizeof(int);
        private const int OffsetKey = OffsetUpdate + sizeof(double);
        private readonly ILogger logger;
        private readonly KeyValueMemoryConfiguration configuration; 
        private ConcurrentDictionary<string, int> keys;


        public static IKeyValueMemory Build(
            KeyValueMemoryConfiguration configuration,
            ILoggerFactory loggerFactory = null)
        {
            return new KeyValueMemory(configuration, loggerFactory);
        }

        public static IKeyValueMemory Build(
            string memoryName,
            int indexCapacity = 0,
            int dataCapacity = 0,
            bool persistent = true,
            ILoggerFactory loggerFactory = null)
        {
            var configuration = new KeyValueMemoryConfiguration
            {
                MemoryName = memoryName,
                IndexCapacity = indexCapacity,
                DataCapacity = dataCapacity,
                Persistent = persistent
            };

            return new KeyValueMemory(configuration, loggerFactory);
        }

        private KeyValueMemory(KeyValueMemoryConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            this.configuration = configuration;
            loggerFactory ??= DeltaX.Configuration.Configuration.DefaultLoggerFactory;
            this.logger = loggerFactory.CreateLogger($"KVM_{configuration.MemoryName}");

            string indexName = $"{configuration.MemoryName}.index";
            string dataName = $"{configuration.MemoryName}.data";

            keys = new ConcurrentDictionary<string, int>();
            mmIndex = new MemoryMappedRecord(indexName, configuration.IndexCapacity, configuration.Persistent, logger);
            mmData = new MemoryMappedRecord(dataName, configuration.DataCapacity, configuration.Persistent, logger);

            InitializeKeys();
        } 


        public event EventHandler<List<string>> KeysChanged;

        /// <summary>
        /// Obtiene la cantidad de claves guardadas
        /// </summary>
        public int Count
        {
            get
            {
                return keys.Count;
            }
        }

        /// <summary>
        /// Obtiene una lista de todas las claves guardadas
        /// </summary>
        public List<string> Keys
        {
            get
            {
                lock (keys)
                {
                    ValidateInitalizeKeysNextPosition();
                    return keys.Keys.ToList();
                }
            }
        }

        private bool IsChangeSizeIndex
        {
            get
            {
                return nextPosition < mmIndex.HeaderSize;
            }
        }

        public KeyValueMemoryConfiguration Configuration => configuration;

        private string GetKey(int position)
        {
            var keyBytes = mmIndex.ReadRecord(position);
            var keyLength = keyBytes.Length - OffsetKey;
            return Encoding.ASCII.GetString(keyBytes, OffsetKey, keyLength);
        }
         
        void InitializeKeys()
        {
            lock (keys)
            {
                keys.Clear();
                var headerSize = mmIndex.HeaderNextPosition;
                foreach (var position in mmIndex.ReadRecordsPositions())
                {
                    string key = GetKey(position);
                    keys[key] = position; 
                }
                nextPosition = headerSize;
            }
        }

        private void ValidateInitalizeKeysNextPosition()
        {
            if (IsChangeSizeIndex)
            {
                lock (keys)
                {
                    var headerSize = mmIndex.HeaderNextPosition;
                    logger?.LogDebug("change header Size: {0} to {1} >>>", nextPosition, headerSize);
                    
                    foreach (var position in mmIndex.ReadRecordsPositions(nextPosition))
                    {
                        string tagName = GetKey(position);
                        keys[tagName] = position;
                        // logger?.LogDebug("    Add key: {0} at {1}", tagName, keys[tagName]);
                        nextPosition = position;
                    }
                    nextPosition = nextPosition > headerSize ? nextPosition : headerSize;
                    logger?.LogInformation("Change key Count: {0} ", keys.Count);
                    
                    KeysChanged?.Invoke(this, Keys);
                } 
            }
        }

        /// <summary>
        /// Escribe los datos de memoria a disco
        /// </summary>
        public void Flush()
        {
            mmIndex.Flush();
            mmData.Flush();
        }

        /// <summary>
        /// Libera los recursos
        /// </summary>
        public void Dispose()
        {
            Flush();
            mmIndex.Dispose();
            mmData.Dispose();
        }

        /// <summary>
        /// Obtiene la fecha de ultimo cambio 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public double GetUpdated(string key)
        {
            return GetUpdate(GetKeyRecordPosition(key));
        }

        /// <summary>
        /// Obtiene la fecha de último cambio
        /// </summary>
        /// <param name="recordPosition"></param>
        /// <returns></returns>
        private double GetUpdate(int recordPosition)
        {
            return recordPosition < 0 ? 0 : mmIndex.ReadDouble(recordPosition + mmIndex.RecordDataOffset + OffsetUpdate);
        }

        int GetKeyRecordPosition(string key)
        {
            if (!keys.ContainsKey(key))
            {
                ValidateInitalizeKeysNextPosition();

                if (!keys.ContainsKey(key))
                {
                    return -1;
                }
            }
            return keys[key];
        }

        /// <summary>
        /// Obtiene el dato asociado a la clave dada
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] GetValue(string key)
        {
            return GetValue(GetKeyRecordPosition(key));
        }

        /// <summary>
        /// Obtiene el registro de datos según el indice de clave
        /// </summary>
        /// <param name="recordPosition"></param>
        /// <returns></returns>
        private byte[] GetValue(int recordPosition)
        {
            if (recordPosition < 0)
                return null;

            int idxValue = mmIndex.ReadInt32(recordPosition + mmIndex.RecordDataOffset);
            return mmData.ReadRecord(idxValue);
        }

        /// <summary>
        /// Escribe el arreglo de bytes en la memoria de datos segun la clave dada
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="addIfNotExist"></param>
        /// <returns>
        /// Devuelve falso si la clave no existe o no puede escribe el dato.
        /// </returns> 
        public bool SetValue(string key, byte[] value, bool addIfNotExist = false)
        {
            var recordPosition = GetKeyRecordPosition(key);
            if (recordPosition < 0)
            {
                // Agrega si no existe en memoria
                if(addIfNotExist)
                {
                    return AddValue(key, value);
                }
                return false;
            }

            int idxValue = mmIndex.ReadInt32(recordPosition + mmIndex.RecordDataOffset);

            int valueIndex = mmData.WriteRecord(idxValue, value, value.Length);
            if (valueIndex < 0)
            {
                if (mmData.GetSizeOfNextRecord(value.Length) <= 0)
                {
                    logger?.LogError("SetValue not enough memory for new record");
                    return false;
                }

                // Borro el dato porque queda obsoleto
                mmData.CleanValue(idxValue);

                // Agrego el dato en una nueva posición
                valueIndex = mmData.AddRecord(value, value.Length);
                if (valueIndex >= 0)
                {
                    // Actualiza el indice
                    mmIndex.Write(recordPosition + mmIndex.RecordDataOffset, (Int32)valueIndex);
                }
            }

            // Actualiza la fecha de modificación
            mmIndex.Write(recordPosition + mmIndex.RecordDataOffset + OffsetUpdate, DateTime.Now.ToUnixTimestamp());

            return valueIndex >= 0;
        }


        /// <summary>
        /// Agrega la clave y valor en las memorias de indices y datos
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddValue(string key, byte[] value)
        {
            if (GetKeyRecordPosition(key) > 0)
            {
                return false;
            }

            var rKey = Encoding.ASCII.GetBytes(key);

            if (mmData.GetSizeOfNextRecord(value.Length) <= 0)
            {
                logger?.LogError("SetValue not enough memory for new data record");
                return false;
            }

            
            if (mmIndex.GetSizeOfNextRecord(rKey.Length + sizeof(int) + sizeof(double)) <= 0)
            {
                logger?.LogError("SetValue not enough memory for new index record");
                return false;
            }

            var valueIndex = mmData.AddRecord(value, value.Length);
            if (valueIndex >= 0)
            {
                List<byte> la = new List<byte>();
                la.AddRange(BitConverter.GetBytes(valueIndex));
                la.AddRange(BitConverter.GetBytes(DateTime.Now.ToUnixTimestamp()));
                la.AddRange(rKey);
                var rawKeyIndex = la.ToArray();

                var headerNextPosition = mmIndex.HeaderNextPosition;
                int idxKey = mmIndex.AddRecord(rawKeyIndex, rawKeyIndex.Length);
                if (idxKey >= 0)
                {
                    lock (key)
                    {
                        keys[key] = idxKey;

                        if (headerNextPosition == nextPosition)
                        {
                            nextPosition = idxKey + mmIndex.ReadInt16(idxKey);
                        }
                    } 
                    return true;
                }
            }

            return false;
        } 
    }
}
