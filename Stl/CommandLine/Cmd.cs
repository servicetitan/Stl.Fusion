using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;

namespace Stl.CommandLine
{
    public abstract class Cmd
    {
        public CliString Executable { get; }
        public CliString WorkingDirectory { get; set; } = CliString.Empty;
        public CliString ArgumentsPrefix { get; set; } = CliString.Empty;
        public ICliFormatter CliFormatter { get; set; } = new CliFormatter();
        public bool EnableErrorValidation { get; set; } = true;

        protected Cmd(CliString executable) => Executable = executable;

        protected virtual Task<ExecutionResult> RunRawAsync(
            object arguments, CliString tail,
            CancellationToken cancellationToken = default) 
            => RunRawAsync(CliFormatter.Format(arguments) + tail, cancellationToken);

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString command, object? arguments, CliString? tail, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync(command + CliFormatter.Format(arguments) + tail, cancellationToken);

        protected virtual Task<ExecutionResult> RunRawAsync(CliString arguments,
            CancellationToken cancellationToken = default)
        {
            var cli = GetCli(cancellationToken);
            return cli
                .SetArguments((ArgumentsPrefix + arguments).Value)
                .ExecuteAsync();
        }

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            string? standardInput,
            CancellationToken cancellationToken = default) 
            => GetCli(cancellationToken)
                .SetArguments((ArgumentsPrefix + arguments).Value)
                .SetStandardInput(standardInput)
                .ExecuteAsync();

        protected virtual ICli GetCli(CancellationToken cancellationToken)
        {
            var cli = Cli.Wrap(Executable.Value)
                .SetCancellationToken(cancellationToken)
                .EnableExitCodeValidation(EnableErrorValidation)
                .EnableStandardErrorValidation(EnableErrorValidation);
            if (!string.IsNullOrEmpty(WorkingDirectory.Value))
                cli.SetWorkingDirectory(WorkingDirectory.Value);
            return cli;
        }

        public override string ToString() => Executable.Value;
    }
}
