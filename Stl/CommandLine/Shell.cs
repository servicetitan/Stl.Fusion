using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;
using Stl.OS;

namespace Stl.CommandLine
{
    [Serializable]
    public class Shell : Cmd
    {
        public static readonly CliString DefaultExecutable = CliString.New("bash").VaryByOS("cmd.exe");
        public static readonly CliString DefaultPrefix = CliString.New("-c").VaryByOS("/C");

        public CliString Prefix { get; set; } = DefaultPrefix;

        public Shell(CliString? executable = null)
            : base(executable ?? DefaultExecutable)
        { }

        public async Task<string> GetOutputAsync(
            CliString command, 
            CancellationToken cancellationToken = default) 
            => (await RunAsync(command, cancellationToken).ConfigureAwait(false))
                .StandardOutput;

        public async Task<string> GetOutputAsync(
            CliString command, 
            string? standardInput,
            CancellationToken cancellationToken = default) 
            => (await RunAsync(command, standardInput, cancellationToken).ConfigureAwait(false))
                .StandardOutput;

        public virtual Task<ExecutionResult> RunAsync(
            CliString command,
            CancellationToken cancellationToken = default) 
            => RunAsync(command, null, cancellationToken);

        public virtual Task<ExecutionResult> RunAsync(
            CliString command,
            string? standardInput,
            CancellationToken cancellationToken = default) 
            => base.RunRawAsync(command, standardInput, cancellationToken);

        protected override CliString TransformArguments(CliString arguments)
            => CmdBuilders.GetShellArguments(arguments, Prefix);
    }
}
