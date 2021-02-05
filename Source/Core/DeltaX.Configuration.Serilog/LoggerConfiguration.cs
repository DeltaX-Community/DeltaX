using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaX.Configuration.Serilog
{
    public class LoggerConfiguration
    {
        static ILoggerFactory defaultLoggerFactory;
        static Microsoft.Extensions.Logging.ILogger defaultLogger;

        public static ILoggerFactory DefaultLoggerFactory
        {
            get
            {
                defaultLoggerFactory ??= GetSerilogLoggerFactory();
                return defaultLoggerFactory;
            }
        }

        public static Microsoft.Extensions.Logging.ILogger DefaultLogger
        {
            get
            { 
                defaultLogger ??= DefaultLoggerFactory.CreateLogger("");
                return defaultLogger;
            }
        }

        public static ILoggerFactory GetSerilogLoggerFactory()
        {
            return new LoggerFactory().AddSerilog();
        }

        public static void SetSerilog(global::Serilog.LoggerConfiguration configuration = null)
        {
            configuration ??= GetSerilogConfiguration();
            Log.Logger = configuration.CreateLogger();
        }

        public static global::Serilog.LoggerConfiguration GetSerilogConfiguration()
        {
            return new global::Serilog.LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.With(new ThreadIdEnricher())
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext}|{ThreadId}|{TaskId} {Message:lj}{NewLine}{Exception}");
        }

        class ThreadIdEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadId", Thread.CurrentThread.ManagedThreadId));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TaskId", Task.CurrentId.HasValue ? Task.CurrentId.ToString() : string.Empty));
            }
        }
    }
}
