using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.IO;

namespace Stl.CommandLine
{
    public interface ICmd
    {
        PathString Executable { get; }
        PathString WorkingDirectory { get; set; }
        ImmutableDictionary<string, string> EnvironmentVariables { get; set; }
        ICliFormatter CliFormatter { get; set; }
        Func<CliString, CliString>? ArgumentTransformer { get; set; }
        ILogger Log { get; set; }
        CmdResultChecks ResultChecks { get; set; }
        bool EchoMode { get; set; }

        Task<ExecutionResult> RunRawAsync(
            CliString arguments, string? standardInput,
            CancellationToken cancellationToken = default);
    }
    
    public abstract class CmdBase : ICmd
    {
        public PathString Executable { get; }
        public PathString WorkingDirectory { get; set; } = PathString.Empty;
        public ImmutableDictionary<string, string> EnvironmentVariables { get; set; } = 
            ImmutableDictionary<string, string>.Empty;
        public ICliFormatter CliFormatter { get; set; } = new CliFormatter();
        public Func<CliString, CliString>? ArgumentTransformer { get; set; } = null;
        public ILogger Log { get; set; } = NullLogger.Instance;
        public CmdResultChecks ResultChecks { get; set; } = CmdResultChecks.NonZeroExitCode;
        public bool EchoMode { get; set; }

        public CmdBase(PathString executable) => Executable = executable;

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
                var echoCommand = "echo" + CmdHelpers.GetEchoArguments(CliString.New(Executable) + arguments);
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
            if (!WorkingDirectory.IsEmpty())
                cli = cli.SetWorkingDirectory(WorkingDirectory);
            return cli;
        }

        protected virtual CliString TransformArguments(CliString arguments)
            => ArgumentTransformer?.Invoke(arguments) ?? arguments;

        public override string ToString() => Executable;
    }
}
