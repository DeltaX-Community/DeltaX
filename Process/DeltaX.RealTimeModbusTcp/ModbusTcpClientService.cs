using DeltaX.RealTime;
using DeltaX.RealTime.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModbusTcp;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ModbusTcpClientService
{
    private readonly ModbusClient modbusClient;
    private readonly ModbusReadConfiguration settings;
    private readonly IRtConnector connector;
    private readonly ProcessInfoStatistics processInfo;
    private readonly ILogger logger;
    private Exception prevException;
    private const int RowSize = 2;

    public ModbusTcpClientService(
        IRtConnector connector,
        ILoggerFactory loggerFactory,
        ProcessInfoStatistics processInfo,
        IOptions<ModbusReadConfiguration> settings)
    {
        this.settings = settings.Value;
        this.modbusClient = new ModbusClient(this.settings.IpAddress, this.settings.Port, this.settings.TimeoutSeconds * 1000);
        this.connector = connector;
        this.processInfo = processInfo;
        this.logger = loggerFactory.CreateLogger(nameof(ModbusTcpClientService));
    }


    private void ParseBlock(ModbusReadBlockConfiguration readBlock, byte[] data)
    {
        foreach (var tag in readBlock.Tags)
        {
            var value = DataParser.Parser(tag.Format, data, tag.BlockIndex * RowSize);

            if (value is string valStr)
            {
                Console.WriteLine("Write String Tag {0} => {1}", tag.TagName, valStr);

                connector.SetText(tag.TagName, valStr);
            }
            else
            {
                var valDbl = Convert.ToDouble(value);
                Console.WriteLine("Write Numeric Tag {0} => {1}", tag.TagName, valDbl);
                connector.SetNumeric(tag.TagName, valDbl);
            }
        }
    }

    private async Task<byte[]> ReadBlockAsync(ModbusReadBlockConfiguration readBlock)
    {
        switch (readBlock.Function)
        {
            case ModbusFunctions.ReadCoilStatus:
                {
                    var result = await modbusClient.ReadCoilsAsync(readBlock.AddressOffset, readBlock.Count);
                    return result;
                }
            case ModbusFunctions.ReadInputStatus:
                {
                    var result = await modbusClient.ReadInputsAsync(readBlock.AddressOffset, readBlock.Count);
                    return result;
                }
            case ModbusFunctions.ReadHoldingRegister:
                {
                    var result = await modbusClient.ReadHoldingRegistersAsync(readBlock.AddressOffset, readBlock.Count);
                    return readBlock.ByteSwap ? result.ReadDataByteSwap() : result.RawData;
                }
            case ModbusFunctions.ReadInputRegister:
                {
                    var result = await modbusClient.ReadInputRegistersAsync(readBlock.AddressOffset, readBlock.Count);
                    return readBlock.ByteSwap ? result.ReadDataByteSwap() : result.RawData;
                }
            default:
                return null;
        }
    }


    public async Task ReadRtTagAsync()
    {
        try
        {
            if (!modbusClient.Connected)
            {
                modbusClient.Init();
            }

            foreach (var block in settings.ReadBlocks)
            {
                var data = await ReadBlockAsync(block);
                ParseBlock(block, data);
            }

            prevException = null;
            processInfo.ScanDateTime = DateTime.Now;
            processInfo.ScanCounter += 1;
            processInfo.ScanRetry = 0;
            processInfo.ScanLastErrror = "";

            await processInfo.SetValuesFromPropertiesAsync(new[] { nameof(processInfo.ScanCounter) });
        }
        catch (Exception e)
        {
            if (e.Message != prevException?.Message)
            {
                logger.LogError(e, "Scan Fail!");
                prevException = e;
                processInfo.ScanLastErrror = e.Message;
            }
            processInfo.ScanRetry += 1;
            processInfo.ScanCounter = 0;
            modbusClient.Terminate();
        }
    }


    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var loopScanInterval = settings?.ScanIntervalMilliSeconds ?? 500;

        connector.Connected += (s, c) =>
        {
            processInfo.ConnectedDateTime = DateTime.Now;
        };

        processInfo.ReadBlocks = settings.ReadBlocks.Length;
        processInfo.TagsCount = settings.ReadBlocks.Sum(b => b.Tags.Count());

        processInfo.LoopPublishStatistics(TimeSpan.FromSeconds(10), stoppingToken);
        connector.ConnectAsync(stoppingToken).Wait();

        return Task.Run(async () =>
        {        
            while (!stoppingToken.IsCancellationRequested)
            {
                processInfo.RunningDateTime = DateTime.Now;
                await ReadRtTagAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(loopScanInterval), stoppingToken);
            }
        }).ContinueWith(t =>
        {
            logger.LogWarning("Process Stoped at: {time}", DateTimeOffset.Now.ToString("o"));
        });
    }
}
