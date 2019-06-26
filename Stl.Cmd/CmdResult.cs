using CliWrap.Models;

namespace Stl.Cmd 
{
    public abstract class CmdResult
    {
        public CmdBuilder Command { get; } 
        public ExecutionResult RawResult { get; }

        protected CmdResult(CmdBuilder command, ExecutionResult rawResult)
        {
            Command = command;
            RawResult = rawResult;
        }

        public override string ToString()
        {
            var result = RawResult.ExitCode == 0 ? "Ok" : $"Error({RawResult.ExitCode})";
            return $"{Command} -> {result}";
        }
    }

    public sealed class RawCmdResult : CmdResult
    {
        public RawCmdResult(CmdBuilder command, ExecutionResult rawResult) 
            : base(command, rawResult) { }
    }
}
