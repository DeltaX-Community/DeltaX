using DeltaX.CommonExtensions;
using DeltaX.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DeltaX.MemoryMappedRecord
{
    /// <summary>
    /// Memoria Mapeada
    /// 
    /// | MMHeader                               (512 bytes) | 
    /// | MMRecordStruct first recored    4 + (data_0 bytes) |
    /// | .................................................. |
    /// | MMRecordStruct N recored        4 + (data_N bytes) |
    /// 
    /// </summary>
    public partial class MemoryMappedRecord : IMemoryMappedRecord
    {
        struct MMHeader
        {
            public double Created_t;
            public double Updated_t;
            public int Size;
            public int Count; 
        }

        unsafe public struct MMRecordStruct
        {
            public Int16 blockSize;
            public Int16 length;
            // public fixed byte data[];
        }

        private MemoryMappedFile mmf;
        private unsafe byte* ptrMemAccessor = (byte*)0;
        private MemoryMappedViewAccessor mmva;
        private unsafe MMHeader* header = (MMHeader*)0;
        private IntPtr mmPtr;
        private readonly string memoryName;
        private readonly ILogger logger;
        private Mutex sharedMutex;

        public int BlockSize { get; set; } = 128;
        public int BlockSizeMin { get; set; } = 16;
        public int HeaderSize { get { unsafe { return header->Size; } } }
        public int HeaderCount { get { unsafe { return header->Count; } } }
        public long HeaderCapacity { get { return mmva.Capacity; } }
        public int HeaderNextPosition { get { return (((HeaderSize + BlockSizeMin - 1) / BlockSizeMin)) * BlockSizeMin; } }
        public int RecordDataOffset { get; } = sizeof(Int16) + sizeof(Int16);


        public MemoryMappedRecord(string memoryName, long capacity = 0, bool persistent = true, ILogger logger = null)
        {
            this.memoryName = memoryName;
            this.logger = logger;
            
            OpenOrCreateMutex();
            
            try
            {
                sharedMutex.WaitOne();
                OpenMMF(memoryName, capacity, persistent);

                if (mmf != null)
                {
                    mmva = mmf.CreateViewAccessor();
                    unsafe
                    {
                        mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref ptrMemAccessor);
                        mmPtr = new IntPtr(ptrMemAccessor);
                    }

                    InitHeader();
                }
            }
            finally
            {
                sharedMutex.ReleaseMutex();
            }
        }

        private void OpenOrCreateMutex()
        {
            if (!Mutex.TryOpenExisting($"{memoryName}.Mutex", out sharedMutex))
            {
                sharedMutex = new Mutex(false, $"{memoryName}.Mutex");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validar la compatibilidad de la plataforma", Justification = "<pendiente>")]
        private void OpenMMF(string memoryName, long capacity = 0, bool persistent = true)
        {
            string fileName = null;
            capacity = capacity > 512 ? capacity : 512;

            if (!persistent)
            {
                if (CommonSettings.IsWindowsOs)
                { 
                    mmf = MemoryMappedFile.CreateOrOpen(memoryName, capacity, MemoryMappedFileAccess.ReadWriteExecute,
                        MemoryMappedFileOptions.None, HandleInheritability.Inheritable); 
                    return;
                }
                else
                {
                    fileName = Path.Combine("/dev/shm", memoryName);
                }
            }

            fileName ??= Path.Combine(CommonSettings.BasePathData, memoryName);
            if (!File.Exists(fileName))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                using (var fd = File.Open(fileName, FileMode.Create))
                {
                    fd.Seek(capacity, SeekOrigin.Begin);
                }
            }

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                if (fs.Length < capacity)
                {
                    logger?.LogWarning("File '{0}' Resize, SetLength from {1} to {2}", fileName, fs.Length, capacity);
                    if (fs.Length == 0)
                    {
                        fs.SetLength(capacity);
                        fs.Write(new byte[512]);
                    }
                    else
                    {
                        fs.SetLength(capacity);
                    }
                }
                mmf = MemoryMappedFile.CreateFromFile(fs, null, fs.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
            }
            logger?.LogInformation("Opened MemoryMappedFile '{0}'", fileName);
        }

        /// <summary>
        /// Inicializa la Cabecera de la memoria
        /// </summary>
        private unsafe void InitHeader()
        {
            header = (MMHeader*)mmPtr.ToPointer();

            if (header->Size == 0)
            {
                header->Created_t = DateTime.Now.ToUnixTimestamp();
                header->Updated_t = DateTime.Now.ToUnixTimestamp();
                header->Size = 512;
                header->Count = 0;
            }
        }

        /// <summary>
        /// Actauliza la cabecera de la memoria
        /// </summary>
        /// <param name="count"></param>
        /// <param name="size"></param>
        private unsafe void UpdateHeader(int count, int size)
        {
            Trace.Assert(count >= 0);
            Trace.Assert(size >= 0);

            header->Updated_t = DateTime.Now.ToUnixTimestamp();
            header->Size = size;
            header->Count = count;
        }


        /// <summary>
        /// Lee un entero de 16 bits (short)  en la posicion dada
        /// </summary> 
        /// <param name="position"></param>
        /// <returns></returns>
        public unsafe Int16 ReadInt16(int position)
        {
            Trace.Assert(position >= 0);
            return *(Int16*)(ptrMemAccessor + position);
        }

        /// <summary>
        /// Lee un entero de 32 bits (long) en la posicion dada
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public unsafe Int32 ReadInt32(int position)
        {
            Trace.Assert(position >= 0);
            return *(Int32*)(ptrMemAccessor + position);
        }

        /// <summary>
        /// Lee un double de 64 bits en la posicion dada
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public unsafe double ReadDouble(int position)
        {
            Trace.Assert(position >= 0);
            return *(double*)(ptrMemAccessor + position);
        }

        /// <summary>
        /// Escribe un valor entero en la posicion dada
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public unsafe void Write(int position, Int16 value)
        {
            Trace.Assert(position >= 0);
            Int16* ptr = (Int16*)(ptrMemAccessor + position);
            *ptr = value;
        }

        /// <summary>
        /// Escribe un valor entero en la posicion dada
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public unsafe void Write(int position, Int32 value)
        {
            Trace.Assert(position >= 0);
            Int32* ptr = (Int32*)(ptrMemAccessor + position);
            *ptr = value;
        }

        /// <summary>
        /// Escribe un valor double en la posicion dada
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public unsafe void Write(int position, double value)
        {
            Trace.Assert(position >= 0);
            double* ptr = (double*)(ptrMemAccessor + position);
            *ptr = value;
        }

        private unsafe void WriteBytes(int position, byte[] data, int length = -1)
        {
            Trace.Assert(position >= 0);
            Trace.Assert(data != null);
            length = length > -1 ? length : data.Length;
            Marshal.Copy(data, 0, mmPtr + position, length);
        }

        /// <summary>
        /// Lee todos los punteros (posiciones) de distintos bloques
        /// Funcion de Bloque
        /// </summary>
        /// <param name="initialPosition"></param>
        /// <returns>
        /// Lista de todos los punteros de cada bloque
        /// </returns>
        public List<int> ReadRecordsPositions(int initialPosition = 512)
        {
            var position = initialPosition > 512 ? initialPosition : 512;

            List<int> values = new List<int>();
            while (position < HeaderSize)
            {
                unsafe
                {
                    MMRecordStruct* rec = (MMRecordStruct*)(ptrMemAccessor + position);
                    if (rec->length > 0)
                    {
                        values.Add(position);
                    }
                    position = position + rec->blockSize;
                }
            }
            return values;
        }

        /// <summary>
        /// Escribe los datos de memoria a disco
        /// </summary>
        public void Flush()
        {
            try
            {
                sharedMutex.WaitOne();
                mmva.Flush();
            }
            finally
            {
                sharedMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Libera los recursos
        /// </summary>
        public void Dispose()
        {
            Flush();
            mmva.Dispose();
            mmf.Dispose();
            sharedMutex.Dispose();
        }

        /// <summary>
        /// Limpia la posicion dada
        /// </summary>
        /// <param name="position"></param>
        public void CleanValue(int position)
        {
            Trace.Assert(position >= 0);
            WriteRecord(position, null, 0);
        }

        /// <summary>
        /// Lee el arreglo de bytes en la posicion dada
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public unsafe byte[] ReadRecord(int position)
        {
            Trace.Assert(position >= 0);
            if (position < HeaderCapacity)
            {
                MMRecordStruct* rec = (MMRecordStruct*)(ptrMemAccessor + position);

                if (rec->length > 0)
                {
                    byte[] arr = new byte[rec->length];
                    Marshal.Copy(mmPtr + position + RecordDataOffset, arr, 0, rec->length);
                    return arr;
                }
            }
            return null;
        }

        /// <summary>
        /// Escribe el arreglo de bytes en el bloque
        /// Valida el tamño de bloque 
        /// Retorna 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <returns>
        /// La posicion en caso de exito de lo contrario -1
        /// </returns>
        public unsafe int WriteRecord(int position, byte[] value, int length = -1)
        {
            Trace.Assert(position >= 0);

            MMRecordStruct* rec = (MMRecordStruct*)(ptrMemAccessor + position);
            if (length + RecordDataOffset <= rec->blockSize)
            { 
                rec->length = length > 0 ? (Int16)length : (Int16)0;
                if (length > 0)
                {
                    WriteBytes(position + RecordDataOffset, value, length);
                }
                return position;
            }
            return -1;
        }

        /// <summary>
        /// Calcula el tamaño del siguiente bloque a reservar 
        /// en funcion del tamaño solicitado
        /// Si no hay espacio suficiente retorna -1
        /// </summary>
        /// <param name="length"></param>
        /// <returns>-1 Si no hay espacio para reservar </returns>
        public int GetSizeOfNextRecord(int length)
        {
            // Obtengo la siguiente posicion bloque
            int position = HeaderNextPosition;

            // Calcula un tamaño para reservar
            int blockSize = length + RecordDataOffset < BlockSizeMin
                ? BlockSizeMin
                : (((length + RecordDataOffset + BlockSize - 1) / BlockSize)) * BlockSize;

            // Valida si es posible usar este espacio
            if (length > 0 && position + blockSize < HeaderCapacity)
            {
                return blockSize;
            }
            return -1;
        }

        /// <summary>
        /// Agrega un nuevo bloque 
        /// El tamaño de bloque está alineado según _BLOCKSIZE
        /// </summary>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <returns>
        /// La posición en caso de éxito, de lo contrario -1
        /// </returns>
        public int AddRecord(byte[] value, int length = -1)
        {
            length = length >= 0 ? length : value.Length;

            try
            {
                sharedMutex.WaitOne();

                // Obtengo la siguiente posicion bloque
                int position = HeaderNextPosition;

                // Calcula un tamaño para reservar
                int blockSize = GetSizeOfNextRecord(length);

                if (blockSize > 0)
                {
                    // actualiza el tamaño reservado para el bloque
                    Write(position, (Int16)blockSize);

                    // actualiza el tamaño usado por el bloque
                    Write(position + sizeof(Int16), (Int16)length);

                    // Posicion de escritura del dato
                    int wPos = position + RecordDataOffset;

                    // Copia datos al bloque
                    WriteBytes(wPos, value, length);

                    // Actualiza la cabecera
                    UpdateHeader(HeaderCount + 1, position + blockSize);
                    return position;
                }
                return -1;
            }
            finally
            {
                sharedMutex.ReleaseMutex();
            }
        }

    }
}



