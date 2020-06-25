using System;
using System.CommandLine;
using System.CommandLine.IO;

namespace Stl.CommandLine
{
    public static class ConsoleEx
    {
        public static IConsole AtomicWriteLine(this IConsole console, string message)
        {
            console.Out.Write(message + Environment.NewLine);
            return console;
        }

        public static IStandardStreamWriter AtomicWriteLine(this IStandardStreamWriter writer, string message)
        {
            writer.Write(message + Environment.NewLine);
            return writer;
        }
    }
}
