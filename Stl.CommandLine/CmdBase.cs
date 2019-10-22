using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.CommandLine
{
    public interface ICmd
    {
        CliString Executable { get; }
        CliString WorkingDirectory { get; set; }
        ImmutableDictionary<string, string> EnvironmentVariables { get; set; }
        ICliFormatter CliFormatter { get; set; }
        ILogger Log { get; set; }
        CmdResultChecks ResultChecks { get; set; }
        bool EchoMode { get; set; }

        Task<ExecutionResult> RunRawAsync(
            CliString arguments, string? standardInput,
            CancellationToken cancellationToken = default);
    }
    
    public abstract class CmdBase : ICmd
    {
        public CliString Executable { get; }
        public CliString WorkingDirectory { get; set; } = CliString.Empty;
        public ImmutableDictionary<string, string> EnvironmentVariables { get; set; } = 
            ImmutableDictionary<string, string>.Empty;
        public ICliFormatter CliFormatter { get; set; } = new CliFormatter();
        public ILogger Log { get; set; } = NullLogger.Instance;
        public CmdResultChecks ResultChecks { get; set; } = CmdResultChecks.NonZeroExitCode;
        public bool EchoMode { get; set; }

        public CmdBase(CliString executable) => Executable = executable;

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

        Task<ExecutionResult> ICmd.RunRawAsync(CliString arguments, string? standardInput, CancellationToken cancellationToken) 
            => RunRawAsync(arguments, standardInput, cancellationToken);
        protected virtual Task<ExecutionResult> RunRawAsync(
            CliString arguments,
            string? standardInput,
            CancellationToken cancellationToken = default)
        {
            arguments = TransformArguments(arguments);
            if (EchoMode) {
                var echoCommand = "echo" + CmdHelpers.GetEchoArguments(Executable + arguments);
                arguments = CmdHelpers.GetShellArguments(echoCommand);
            }
            
            var cli = GetCli(cancellationToken)
                .SetArguments(arguments.Value);
            if (standardInput != null)
                cli = cli.SetStandardInput(standardInput);
            foreach (var (key, value) in EnvironmentVariables)
                cli = cli.SetEnvironmentVariable(key, value);
            Log?.LogDebug($"Running: {WorkingDirectory}: {Executable} {arguments}");
            return cli.ExecuteAsync();
        }

        protected virtual ICli GetCli(CancellationToken cancellationToken)
        {
            var cli = Cli.Wrap((EchoMode ? ShellCmd.DefaultExecutable : Executable).Value)
                .SetCancellationToken(cancellationToken)
                .EnableExitCodeValidation(ResultChecks.HasFlag(CmdResultChecks.NonZeroExitCode))
                .EnableStandardErrorValidation(ResultChecks.HasFlag(CmdResultChecks.NonEmptyStandardError));
            if (!string.IsNullOrEmpty(WorkingDirectory.Value))
                cli = cli.SetWorkingDirectory(WorkingDirectory.Value);
            return cli;
        }

        protected virtual CliString TransformArguments(CliString arguments)
            => arguments;

        public override string ToString() => Executable.Value;
    }
}
