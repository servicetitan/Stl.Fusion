using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;

namespace Stl.CommandLine
{
    public class Shell : Cmd
    {
        public static readonly CmdPart DefaultExecutable = CmdPart.New("bash").VaryByOS("cmd.exe");
        public static readonly CmdPart DefaultPrefix = CmdPart.New("-c").VaryByOS("/C");

        protected override CmdPart GetExecutable() => DefaultExecutable;
        protected virtual CmdPart GetPrefix() => DefaultPrefix;

        public virtual Task<ExecutionResult> RunAsync(
            CmdPart command, 
            CancellationToken cancellationToken = default)
        {
            var arguments = GetPrefix() + command.Quote(); 
            return base.RunRawAsync(arguments, cancellationToken);
        }

        public virtual async Task<string> GetOutputAsync(
            CmdPart command, 
            CancellationToken cancellationToken = default) 
            => (await RunAsync(command, cancellationToken)).StandardOutput;
    }
}
