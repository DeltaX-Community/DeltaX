using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaX.Configuration.Serilog
{
    public class LoggerConfiguration
    { 
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
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext}|{ThreadId}|{TaskId} {Message:lj}{NewLine}{Exception}",
                    theme: GetDefaultConsoleTheme());
        }

        public static AnsiConsoleTheme GetDefaultConsoleTheme()
        {
            /// Ver colores en:
            /// http://www.lihaoyi.com/post/BuildyourownCommandLinewithANSIescapecodes.html
            /// 
            return new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
            {
                [ConsoleThemeStyle.Text] = "\x1b[38;5;0253m",
                [ConsoleThemeStyle.SecondaryText] = "\x1b[38;5;0246m",
                [ConsoleThemeStyle.TertiaryText] = "\x1b[38;5;0242m",
                [ConsoleThemeStyle.Invalid] = "\x1b[33;1m",
                [ConsoleThemeStyle.Null] = "\x1b[38;5;0038m",
                [ConsoleThemeStyle.Name] = "\x1b[38;5;0081m",
                [ConsoleThemeStyle.String] = "\x1b[38;5;0216m",
                [ConsoleThemeStyle.Number] = "\x1b[38;5;151m",
                [ConsoleThemeStyle.Boolean] = "\x1b[38;5;0038m",
                [ConsoleThemeStyle.Scalar] = "\x1b[38;5;0079m",
                [ConsoleThemeStyle.LevelVerbose] = "\x1b[37m\u001b[7m",
                [ConsoleThemeStyle.LevelDebug] = "\x1b[44;1m\x1b[37m",
                [ConsoleThemeStyle.LevelInformation] = "\x1b[32m",
                [ConsoleThemeStyle.LevelWarning] = "\x1b[38;5;227m",
                [ConsoleThemeStyle.LevelError] = "\x1b[38;5;255m\x1b[48;5;160m",
                [ConsoleThemeStyle.LevelFatal] = "\x1b[41;1m\x1b[37m",
            });
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
