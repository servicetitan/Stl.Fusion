using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;

namespace Stl.CommandLine
{
    public abstract class Cmd
    {
        protected virtual Task<ExecutionResult> RunRawAsync(
            object arguments, CliString tail,
            CancellationToken cancellationToken = default) 
            => RunRawAsync(GetCliFormatter().Format(arguments) + tail, cancellationToken);

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString command, object arguments, CliString tail, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync(command + GetCliFormatter().Format(arguments) + tail, cancellationToken);

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            CancellationToken cancellationToken = default) 
            => GetCli(cancellationToken)
                .SetArguments((GetArgumentsPrefix() + arguments).Value)
                .ExecuteAsync();

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            string standardInput,
            CancellationToken cancellationToken = default) 
            => GetCli(cancellationToken)
                .SetArguments((GetArgumentsPrefix() + arguments).Value)
                .SetStandardInput(standardInput)
                .ExecuteAsync();

        protected abstract CliString GetExecutable();
        protected virtual CliString GetArgumentsPrefix() => "";
        
        protected virtual ICliFormatter GetCliFormatter() => new CliFormatter();

        protected virtual ICli GetCli(CancellationToken cancellationToken) 
            => Cli.Wrap(GetExecutable().Value)
                .SetCancellationToken(cancellationToken);

        public override string ToString() => GetExecutable().Value;
    }
}
