using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DeltaX.RealTime.Decorators;
using DeltaX.RealTime.Interfaces;
using DeltaX.RealTime;
 
public class SimulationService
{
    private readonly ILogger logger;
    private readonly IRtConnector rt; 
    private RtTagType<bool> actionReset;
    private RtTagType<bool> actionStart;
    private RtTagType<bool> actionStop;
    private RtTagType<bool> running;
    private RtTagType<bool> slowRunning; 
    private RtTagType<int> countTotal;
    private RtTagType<int> countGood;
    private RtTagType<int> countBad;
    private RtTagType<double> lineSpeed;  
    private RtTagType<double> lineSpeedMin;  
    private RtTagType<double> lineSpeedMax;
    private RtTagType<double> lineSpeedSetPoint;
    private RtTagType<double> lineLength;
    private RtTagType<double> piecesByLength;
    private RtTagType<int> percentBadOverGood;
    private bool simulatorRunning;
    private TimeSpan cycleTime;

    public SimulationService(
        ILogger<SimulationService> logger,
        IRtConnector rt)
    {
        this.logger = logger;
        this.rt = rt;
        cycleTime = TimeSpan.FromMilliseconds(200);

        Init();
    }

    private void Init(string tagPrefix = "simulation/")
    {
        actionReset = rt.GetOrAddTag<bool>($"{tagPrefix}actionReset");
        actionStart = rt.GetOrAddTag<bool>($"{tagPrefix}actionStart");
        actionStop = rt.GetOrAddTag<bool>($"{tagPrefix}actionStop");
        running = rt.GetOrAddTag<bool>($"{tagPrefix}running");
        slowRunning = rt.GetOrAddTag<bool>($"{tagPrefix}slowRunning"); 
        countTotal = rt.GetOrAddTag<int>($"{tagPrefix}countTotal");
        countGood = rt.GetOrAddTag<int>($"{tagPrefix}countGood");
        countBad = rt.GetOrAddTag<int>($"{tagPrefix}countBad");
        lineSpeed = rt.GetOrAddTag<double>($"{tagPrefix}lineSpeed");
        lineSpeedMin = rt.GetOrAddTag<double>($"{tagPrefix}lineSpeedMin");
        lineSpeedMax = rt.GetOrAddTag<double>($"{tagPrefix}lineSpeedMax");
        lineSpeedSetPoint = rt.GetOrAddTag<double>($"{tagPrefix}lineSpeedSetPoint");
        lineLength = rt.GetOrAddTag<double>($"{tagPrefix}lineLength");
        piecesByLength = rt.GetOrAddTag<double>($"{tagPrefix}piecesByLength");
        percentBadOverGood = rt.GetOrAddTag<int>($"{tagPrefix}percentBadOverGood"); 
    }


    private void CreateTagIfNull()
    {
        if (rt.IsConnected)
        {
            if (!actionReset.Status) actionReset.Set(default);
            if (!actionStart.Status) actionStart.Set(default);
            if (!actionStop.Status) actionStop.Set(default);
            if (!running.Status) running.Set(default);
            if (!slowRunning.Status) slowRunning.Set(default); 
            if (!countTotal.Status) countTotal.Set(default); 
            if (!countGood.Status) countGood.Set(default); 
            if (!countBad.Status) countBad.Set(default); 
            if (!lineSpeed.Status) lineSpeed.Set(default); 
            if (!lineSpeedMin.Status) lineSpeedMin.Set(10); 
            if (!lineSpeedMax.Status) lineSpeedMax.Set(100); 
            if (!lineSpeedSetPoint.Status) lineSpeedSetPoint.Set(50); 
            if (!lineLength.Status) lineLength.Set(default); 
            if (!piecesByLength.Status) piecesByLength.Set(default); 
            if (!percentBadOverGood.Status) percentBadOverGood.Set(20);
             
            simulatorRunning = running.Value;
        }
    }
     

    Random random = new Random();
    private double GetRandom(double min, double max)
    {
        return random.NextDouble() * (max - min) + min;
    }

    private void UpdateLineSpeed()
    {
        if (simulatorRunning)
        {
            if (lineSpeed.Value < lineSpeedMin.Value)
            {
                lineSpeed.Set(lineSpeed.Value + GetRandom(0, lineSpeedMin.Value / 5));
            }
            else if (lineSpeed.Value > lineSpeedMax.Value)
            {
                lineSpeed.Set(lineSpeed.Value - GetRandom(0, lineSpeedMin.Value / 20));
            }
            else
            {
                // lineSpeed.Set(lineSpeed.Value + GetRandom(-lineSpeedMin.Value / 20, lineSpeedMin.Value / 20));

                var diffSpeed = (lineSpeed.Value - lineSpeedSetPoint.Value) * 0.1;
                var tolerance = lineSpeedSetPoint.Value * 0.02;
                if (diffSpeed < 0)
                {
                    lineSpeed.Set(lineSpeed.Value + GetRandom(0, -diffSpeed) + GetRandom(-tolerance, tolerance));
                }
                else
                {
                    lineSpeed.Set(lineSpeed.Value - GetRandom(0, diffSpeed) + GetRandom(-tolerance, tolerance));
                }
            }
        }
        else
        {
            if (lineSpeed.Value > lineSpeedMax.Value / 20)
            {
                lineSpeed.Set(lineSpeed.Value - GetRandom(0, lineSpeedMax.Value / 20));
            }
            else if (lineSpeed.Value != 0)
            {
                lineSpeed.Set(0);
            }
        }
    }

    private void Simulte()
    {
        logger.LogDebug("Simulte...");

        // Reset
        if (actionReset.Value)
        {
            simulatorRunning = false;
            actionReset.Set(false);
            countBad.Set(0);
            countGood.Set(0);
            countTotal.Set(0);
            lineLength.Set(0);
            lineSpeed.Set(0);
            running.Set(false);
        }

        // Start
        if (actionStart.Value)
        {
            simulatorRunning = true;
            actionStart.Set(false);
        }

        // Stop
        if (actionStop.Value)
        {
            simulatorRunning = false;
            actionStop.Set(false);
        } 

        // Line Speed
        UpdateLineSpeed();

        // Runing
        var isRunning = lineSpeed.Value > lineSpeedMin.Value / 5;
        if (running.Value != isRunning)
        {
            running.Set(isRunning);
        }

        // Slow
        var isSlow = isRunning && lineSpeed.Value < lineSpeedMin.Value;
        if (slowRunning.Value != isSlow)
        {
            slowRunning.Set(isSlow);
        }

        if (simulatorRunning)
        {
            // Length
            var deltaLength = lineSpeed.Value * (cycleTime.TotalSeconds);
            lineLength.Set(lineLength.Value + deltaLength);

            // Number of Pieces 
            var newTotal = (int)Math.Truncate(lineLength.Value * piecesByLength.Value);
            if (countTotal.Value != newTotal)
            {
                var deltaPieces = newTotal - countTotal.Value;
                if (GetRandom(0, 100) > percentBadOverGood.Value)
                {
                    countGood.Set(countGood.Value + deltaPieces);
                }
                else
                {
                    countBad.Set(countBad.Value + deltaPieces);
                }
                countTotal.Set(newTotal);
            }
        }
    }

    public Task RunAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            logger.LogInformation("Simulator Started.");
            
            await Task.Delay(cycleTime, stoppingToken);

            CreateTagIfNull();

            while (!stoppingToken.IsCancellationRequested)
            {
                Simulte();

                await Task.Delay(cycleTime, stoppingToken);
            }
        });
    }
}