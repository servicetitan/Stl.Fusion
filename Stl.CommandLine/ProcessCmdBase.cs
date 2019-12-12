using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;
using Microsoft.Extensions.Logging;
using Stl.IO;

namespace Stl.CommandLine 
{
    public interface IProcessCmd : ICmd
    {
        PathString Executable { get; }
        PathString WorkingDirectory { get; set; }
        ImmutableDictionary<string, string> EnvironmentVariables { get; set; }
    }

    public abstract class ProcessCmdBase : CmdBase, IProcessCmd
    {
        public PathString Executable { get; }
        public PathString WorkingDirectory { get; set; } = PathString.Empty;
        public ImmutableDictionary<string, string> EnvironmentVariables { get; set; } = 
            ImmutableDictionary<string, string>.Empty;

        protected ProcessCmdBase(PathString executable) => Executable = executable;

        public override string ToString() => $"{GetType().Name}(\"{Executable}\" @ \"{WorkingDirectory}\")";

        protected override Task<ExecutionResult> RunRawAsyncImpl(
            CliString arguments, string? standardInput, 
            CancellationToken cancellationToken)
        {
            var cli = GetCli(cancellationToken)
                .SetArguments(arguments.Value);
            if (standardInput != null)
                cli = cli.SetStandardInput(standardInput);
            foreach (var (key, value) in EnvironmentVariables)
                cli = cli.SetEnvironmentVariable(key, value);
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
    }
}
