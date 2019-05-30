using System.IO;
using Serilog;
using Serilog.Events;

namespace Stl.Testing
{
    public static class Test
    {
        public static readonly string DefaultTestLoggerOutputTemplate = 
            "### [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
        public static readonly string DefaultConsoleLoggerOutputTemplate = 
            "{Timestamp:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
        
        public static (ILogger Log, StringWriter Writer) CreateLog(
            string? outputTemplate = null, 
            LogEventLevel minimumLevel = LogEventLevel.Debug,
            bool logToConsole = true,
            string? consoleOutputTemplate = null)
        {
            outputTemplate ??= DefaultTestLoggerOutputTemplate;
            consoleOutputTemplate ??= DefaultConsoleLoggerOutputTemplate;
            var writer = new StringWriter();
            var logCfg = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .WriteTo.TextWriter(writer, outputTemplate: outputTemplate);
            if (logToConsole)
                logCfg = logCfg.WriteTo.Console(outputTemplate: consoleOutputTemplate);
            var log = logCfg.CreateLogger();
            return (log, writer);
        }
    }
}
