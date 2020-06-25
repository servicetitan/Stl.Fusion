using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
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

        protected override async Task<CmdResult> RunRawAsyncImpl(
            CliString arguments, string? standardInput, 
            CancellationToken cancellationToken)
        {
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            var command = Cli.Wrap((EchoMode ? ShellCmd.DefaultExecutable : Executable).Value)
                .WithValidation(ResultValidation)
                .WithArguments(arguments.Value)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outputBuilder))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errorBuilder));
            if (standardInput != null)
                command = command.WithStandardInputPipe(PipeSource.FromString(standardInput));
            if (!WorkingDirectory.IsEmpty)
                command = command.WithWorkingDirectory(WorkingDirectory);
            if (!EnvironmentVariables.IsEmpty)
                command = command.WithEnvironmentVariables(EnvironmentVariables);
            command = Configure(command);
            
            var result = await command.ExecuteAsync();
            return new CmdResult(command, result, outputBuilder, errorBuilder);
        }

        protected virtual Command Configure(Command command) => command;
    }
}
