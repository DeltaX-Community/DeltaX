using DeltaX.CommonExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaX.MemoryMappedRecord
{
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

        /// <summary>
        /// tags contiene el 
        /// Donde  
        ///     Key: string clave 
        ///     indexOffset: posición en memoria donde esta alojada la referencia al dato.
        /// </summary>
        private Dictionary<string, int> keys = new Dictionary<string, int>();

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
                ValidateInitalizeKeysNextPosition();
                return keys.Keys.ToList();
            }
        }

        /// <summary>
        /// Inicializa la memoria
        /// 
        /// Craa dos memorias, una para indice y otra para datos
        /// </summary>
        /// <param name="MemoryName"></param>
        /// <param name="indexCapacity"></param>
        /// <param name="dataCapacity"></param>
        /// <param name="persistent"></param>
        public KeyValueMemory(string MemoryName, int indexCapacity = 1000000 * 128, int dataCapacity = 200 * 1024 * 1024, bool persistent = true)
        {
            string indexName = $"{MemoryName}.index";
            string dataName = $"{MemoryName}.data";

            mmIndex = new MemoryMappedRecord(indexName, indexCapacity, persistent);
            mmData = new MemoryMappedRecord(dataName, dataCapacity, persistent);

            InitializeKeys();
        }
         
        private bool IsChangeSizeIndex
        {
            get
            {
                return nextPosition < mmIndex.HeaderSize;
            }
        }

        /// <summary>
        /// Obtiene el nombre de clave guardada
        /// </summary>
        /// <param name="position"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private string GetKey(int position)
        {
            var keyBytes = mmIndex.ReadRecord(position);
            var keyLength = keyBytes.Length - OffsetKey;
            return Encoding.ASCII.GetString(keyBytes, OffsetKey, keyLength);
        }
         
        void InitializeKeys()
        {
            keys.Clear();
            foreach (var position in mmIndex.ReadRecordsPositions())
            { 
                string key = GetKey(position); 
                keys[key] = position;
            }
            nextPosition = mmIndex.HeaderNextPosition;
        }

        /// <summary>
        /// Valida e inicializa las claves agregadas 
        /// </summary>
        private void ValidateInitalizeKeysNextPosition()
        {
            if (IsChangeSizeIndex)
            {
                Console.WriteLine("change header Size: {0} > {1} >>>", nextPosition, mmIndex.HeaderNextPosition);
                foreach (var position in mmIndex.ReadRecordsPositions(nextPosition))
                { 
                    string tagName = GetKey(position);
                    keys[tagName] = position;
                    Console.WriteLine("    Add key: {0} at {1}", tagName, keys[tagName]);
                }
                nextPosition = mmIndex.HeaderNextPosition;
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
                    Console.WriteLine("SetValue No hay espacio en memoria de Datos para este registro");
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
            ValidateInitalizeKeysNextPosition();

            if (keys.ContainsKey(key))
                return false;

            var rKey = Encoding.ASCII.GetBytes(key);

            if (mmData.GetSizeOfNextRecord(value.Length) <= 0)
            {
                Console.WriteLine("No hay espacio en memoria de Datos para este registro");
                return false;
            }

            if (mmIndex.GetSizeOfNextRecord(rKey.Length + sizeof(int) + sizeof(double)) <= 0)
            {
                Console.WriteLine("No hay espacio en memoria de Indice para este registro");
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

                int idxKey = mmIndex.AddRecord(rawKeyIndex, rawKeyIndex.Length);
                if (idxKey >= 0)
                {
                    keys[key] = idxKey;

                    nextPosition = mmIndex.HeaderNextPosition;
                    return true;
                }
            }

            return false;
        } 
    }
}
