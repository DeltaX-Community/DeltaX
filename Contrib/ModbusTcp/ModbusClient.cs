using System;
using System.Net.Sockets;
using ModbusTcp.Protocol.Request;
using System.Threading.Tasks;
using ModbusTcp.Protocol;
using System.Linq;
using ModbusTcp.Protocol.Reply;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace ModbusTcp
{
    public class ModbusClient
    {
        public bool Connected => tcpClient?.Connected ?? false;

        private int socketTimeout;
        private readonly int port;
        private TcpClient tcpClient;
        private NetworkStream transportStream;
        private readonly string ipAddress;

        // Let's wait for 60 seconds for the socket if socketTimeout
        // isn't passed by caller
        public ModbusClient(string ipAddress, int port, int socketTimeout = 60000)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.socketTimeout = socketTimeout;
        }

        public void Init()
        {
            tcpClient = new TcpClient(ipAddress, port);
            transportStream = tcpClient.GetStream();
        }

        /// <summary>
        /// Reads words holding registers
        /// </summary>
        /// <param name="offset">The register offset</param>
        /// <param name="count">Number of words to read</param>
        /// <returns>The words read</returns>
        public async Task<IModbusReadResponseBase> ReadHoldingRegistersAsync(int offset, int count, byte unit = 0x01)
        {
            if (tcpClient == null)
                throw new Exception("Object not intialized");

            var request = new ModbusRequest03(offset, count, unit);
            var buffer = request.ToNetworkBuffer();

            using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
            {
                using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                {
                    await transportStream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                }
            }
            var response = await ReadResponseAsync<ModbusReply03>();

            return response;
        } 

        /// <summary>
        /// Reads words input registers
        /// </summary>
        /// <param name="offset">The register offset</param>
        /// <param name="count">Number of words to read</param>
        /// <returns>The words read</returns>
        public async Task<IModbusReadResponseBase> ReadInputRegistersAsync(int offset, int count, byte unit = 0x01)
        {
            if (tcpClient == null)
                throw new Exception("Object not intialized");

            var request = new ModbusRequest04(offset, count, unit);
            var buffer = request.ToNetworkBuffer();

            using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
            {
                using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                {
                    await transportStream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                }
            }
            var response = await ReadResponseAsync<ModbusReply04>();
            return response;
        }
         
        /// <summary>
        /// Reads words input registers
        /// </summary>
        /// <param name="offset">The register offset</param>
        /// <param name="count">Number of words to read</param>
        /// <returns>The words read</returns>
        public async Task<byte[]> ReadInputsAsync(int offset, int count, byte unit = 0x01)
        {
            if (tcpClient == null)
                throw new Exception("Object not intialized");

            var request = new ModbusRequest02(offset, count, unit);
            var buffer = request.ToNetworkBuffer();

            using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
            {
                using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                {
                    await transportStream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                }
            }
            var response = await ReadResponseAsync<ModbusReply02>();
            return response.RawData;
        }
         

        /// <summary>
        /// Reads words input registers
        /// </summary>
        /// <param name="offset">The register offset</param>
        /// <param name="count">Number of words to read</param>
        /// <returns>The words read</returns>
        public async Task<byte[]> ReadCoilsAsync(int offset, int count, byte unit = 0x01)
        {
            if (tcpClient == null)
                throw new Exception("Object not intialized");

            var request = new ModbusRequest01(offset, count, unit);
            var buffer = request.ToNetworkBuffer();

            using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
            {
                using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                {
                    await transportStream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                }
            }
            var response = await ReadResponseAsync<ModbusReply01>();
            return response.RawData;
        }  

        /// <summary>
        /// Writes floats to holding registers
        /// </summary>
        /// <param name="offset">The first register offset</param>
        /// <param name="values">The values to write</param>
        /// <returns>Awaitable task</returns>
        public async Task WriteRegistersAsync(int offset, float[] values, byte unit = 0x01)
        {
            if (tcpClient == null)
                throw new Exception("Object not intialized");

            var request = new ModbusRequest16(offset, values, unit);
            var buffer = request.ToNetworkBuffer();

            using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
            {
                using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                {
                    await transportStream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                }
            }
            var response = await ReadResponseAsync<ModbusReply16>();
        }

        /// <summary>
        /// Writes words to holding registers
        /// </summary>
        /// <param name="offset">The first register offset</param>
        /// <param name="values">The values to write</param>
        /// <returns>Awaitable task</returns>
        public async Task WriteRegistersAsync(int offset, short[] values, byte unit = 0x01)
        {
            if (tcpClient == null)
                throw new Exception("Object not intialized");

            var request = new ModbusRequest16(unit);
            request.WordCount = (short)(values.Length * 2);
            request.RegisterValues = values.ToNetworkBytes();

            var buffer = request.ToNetworkBuffer();

            using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
            {
                using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                {
                    await transportStream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                }
            }

            await ReadResponseAsync<ModbusReply16>();
        }
         

        /// <summary>
        /// Writes bit to coil
        /// </summary>
        /// <param name="offset">The first register offset</param>
        /// <param name="values">The values to write</param>
        /// <returns>Awaitable task</returns>
        public async Task WriteCoilAsync(int offset, bool value, byte unit = 0x01)
        {
            if (tcpClient == null)
                throw new Exception("Object not intialized");

            var request = new ModbusRequest05(unit, value, unit);

            var buffer = request.ToNetworkBuffer();

            using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
            {
                using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                {
                    await transportStream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                }
            }

            await ReadResponseAsync<ModbusReply05>();
        } 

        /// <summary>
        /// Writes single register to holding registers
        /// </summary>
        /// <param name="offset">The first register offset</param>
        /// <param name="values">The values to write</param>
        /// <returns>Awaitable task</returns>
        public async Task WriteRegisterAsync(int offset, short value, byte unit = 0x01)
        {
            if (tcpClient == null)
                throw new Exception("Object not intialized");

            var request = new ModbusRequest06(unit, value, unit);

            var buffer = request.ToNetworkBuffer();

            using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
            {
                using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                {
                    await transportStream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                }
            }

            await ReadResponseAsync<ModbusReply06>();
        } 
          
        private async Task<T> ReadResponseAsync<T>() where T : ModbusResponseBase
        {
            var headerBytes = await ReadFromBufferAsync(ModbusHeader.FixedLength);
            var header = ModbusHeader.FromNetworkBuffer(headerBytes);

            var dataBytes = await ReadFromBufferAsync(header.Length);

            var fullBuffer = headerBytes.Concat(dataBytes).ToArray();
            var response = Activator.CreateInstance<T>();
            response.FromNetworkBuffer(fullBuffer);

            return response;
        }
          
        private async Task<byte[]> ReadFromBufferAsync(int totalSize)
        {
            var buffer = new byte[totalSize];

            var idx = 0;
            var remainder = totalSize;

            while (remainder > 0)
            {
                int readBytes = 0;
                using (var cancellationTokenSource = new CancellationTokenSource(socketTimeout))
                {
                    using (cancellationTokenSource.Token.Register(() => transportStream.Close()))
                    {
                        readBytes = await transportStream.ReadAsync(buffer, idx, remainder, cancellationTokenSource.Token);
                    }
                }
                remainder -= readBytes;
                idx += readBytes;

                if (readBytes == 0)
                    throw new SocketException((int)SocketError.ConnectionReset);
            }

            return buffer;
        }

        /// <summary>
        /// Terminates the session
        /// </summary>
        public void Terminate()
        {
            if (transportStream != null)
            {
                transportStream.Close();
                transportStream.Dispose();
                transportStream = null;
            }
            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient.Dispose();
                tcpClient = null;
            }
        }
    }
}
