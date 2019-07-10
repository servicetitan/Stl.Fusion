using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;

namespace Stl.CommandLine
{
    public abstract class Cmd
    {
        protected CliString Executable { get; }
        protected CliString ArgumentsPrefix { get; }
        protected ICliFormatter CliFormatter { get; }

        protected Cmd(CliString executable, CliString argumentsPrefix = default, ICliFormatter? cliFormatter = null)
        {
            Executable = executable;
            ArgumentsPrefix = argumentsPrefix;
            CliFormatter = cliFormatter ?? new CliFormatter();
        }

        protected virtual Task<ExecutionResult> RunRawAsync(
            object arguments, CliString tail,
            CancellationToken cancellationToken = default) 
            => RunRawAsync(CliFormatter.Format(arguments) + tail, cancellationToken);

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString command, object arguments, CliString tail, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync(command + CliFormatter.Format(arguments) + tail, cancellationToken);

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            CancellationToken cancellationToken = default) 
            => GetCli(cancellationToken)
                .SetArguments((ArgumentsPrefix + arguments).Value)
                .ExecuteAsync();

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            string standardInput,
            CancellationToken cancellationToken = default) 
            => GetCli(cancellationToken)
                .SetArguments((ArgumentsPrefix + arguments).Value)
                .SetStandardInput(standardInput)
                .ExecuteAsync();

        protected virtual ICli GetCli(CancellationToken cancellationToken) 
            => Cli.Wrap(Executable.Value)
                .SetCancellationToken(cancellationToken);

        public override string ToString() => Executable.Value;
    }
}
