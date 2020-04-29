using System.IO;
using Serilog;
using Serilog.Events;

namespace Stl.Testing
{
    public static class TestLogger
    {
        public static readonly string DefaultTestLoggerOutputTemplate = 
            "### [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
        public static readonly string DefaultConsoleLoggerOutputTemplate = 
            "{Timestamp:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
        
        public static ILogger New(
            TextWriter writer,
            string? outputTemplate = null, 
            LogEventLevel minimumLevel = LogEventLevel.Debug,
            bool logToConsole = true,
            string? consoleOutputTemplate = null)
        {
            outputTemplate ??= DefaultTestLoggerOutputTemplate;
            consoleOutputTemplate ??= DefaultConsoleLoggerOutputTemplate;
            var logCfg = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .WriteTo.TextWriter(writer, outputTemplate: outputTemplate);
            if (logToConsole)
                logCfg = logCfg.WriteTo.Console(outputTemplate: consoleOutputTemplate);
            return logCfg.CreateLogger();
        }
    }
}
