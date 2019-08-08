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
        public ICliFormatter CliFormatter { get; set; } = new CliFormatter();
        public bool EnableErrorValidation { get; set; } = true;
        public bool EchoMode { get; set; }

        protected Cmd(CliString executable) => Executable = executable;

        protected Task<ExecutionResult> RunRawAsync(
            object? arguments, CliString tail = default,
            CancellationToken cancellationToken = default) 
            => RunRawAsync("", arguments, tail, cancellationToken);

        protected Task<ExecutionResult> RunRawAsync(
            CliString command, object? arguments, CliString tail = default, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync(command + CliFormatter.Format(arguments) + tail, cancellationToken);

        protected Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            CancellationToken cancellationToken = default)
            => RunRawAsync(arguments, null, cancellationToken);

        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            string? standardInput,
            CancellationToken cancellationToken = default)
        {
            arguments = TransformArguments(arguments);
            if (EchoMode)
                arguments = Shell.DefaultPrefix + ("echo" + (Executable + arguments).Quote()).Quote();
            
            var cli = GetCli(cancellationToken)
                .SetArguments(arguments.Value);
            if (standardInput != null)
                cli = cli.SetStandardInput(standardInput);
            return cli.ExecuteAsync();
        }

        protected virtual ICli GetCli(CancellationToken cancellationToken)
        {
            var cli = Cli.Wrap((EchoMode ? Shell.DefaultExecutable : Executable).Value)
                .SetCancellationToken(cancellationToken)
                .EnableExitCodeValidation(EnableErrorValidation)
                .EnableStandardErrorValidation(EnableErrorValidation);
            if (!string.IsNullOrEmpty(WorkingDirectory.Value))
                cli = cli.SetWorkingDirectory(WorkingDirectory.Value);
            return cli;
        }

        protected virtual CliString TransformArguments(CliString arguments)
            => arguments;

        public override string ToString() => Executable.Value;
    }
}
