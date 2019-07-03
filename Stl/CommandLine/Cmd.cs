using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;

namespace Stl.CommandLine
{
    public abstract class Cmd
    {
        protected Task<ExecutionResult> RunRawAsync(
            CmdPart arguments,
            CancellationToken cancellationToken = default) 
            => GetCli(cancellationToken)
                .SetArguments((string) arguments ?? "")
                .ExecuteAsync();

        protected Task<ExecutionResult> RunRawAsync(
            CmdPart arguments,
            string standardInput,
            CancellationToken cancellationToken = default) 
            => GetCli(cancellationToken)
                .SetArguments((string) arguments ?? "")
                .SetStandardInput(standardInput)
                .ExecuteAsync();

        protected abstract CmdPart GetExecutable();
        
        protected virtual ICli GetCli(CancellationToken cancellationToken) 
            => Cli.Wrap((string) GetExecutable())
                .SetCancellationToken(cancellationToken);

        public override string ToString() => (string) GetExecutable();
    }
}
