using System;
using System.Text;
using CliWrap;

namespace Stl.CommandLine
{
    public class CmdResult : CommandResult
    {
        public Command Command { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }

        public CmdResult(Command command, CommandResult result, 
            StringBuilder? standardOutput = null,
            StringBuilder? standardError = null) 
            : this(command, 
                result.ExitCode, result.StartTime, result.ExitTime, 
                standardOutput, standardError)
        { }

        public CmdResult(Command command, 
            int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime,
            StringBuilder? standardOutput = null,
            StringBuilder? standardError = null) 
            : base(exitCode, startTime, exitTime)
        {
            Command = command;
            StandardOutput = standardOutput?.ToString() ?? "";
            StandardError = standardError?.ToString() ?? "";
        }

        public CmdResult(Command command, 
            int exitCode, DateTimeOffset startTime, DateTimeOffset exitTime,
            string? standardOutput = null,
            string? standardError = null) 
            : base(exitCode, startTime, exitTime)
        {
            Command = command;
            StandardOutput = standardOutput ?? "";
            StandardError = standardError ?? "";
        }
    }
}
