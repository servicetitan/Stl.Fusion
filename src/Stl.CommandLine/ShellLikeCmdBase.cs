using System.Threading;
using System.Threading.Tasks;
using CliWrap;

namespace Stl.CommandLine
{
    public abstract class ShellLikeCmdBase : ProcessCmdBase
    {
        protected ShellLikeCmdBase(CliString executable) : base(executable) { }

        protected virtual CliString GetPrefix() => CliString.Empty;
        
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

        public virtual Task<CmdResult> RunAsync(
            CliString command,
            CancellationToken cancellationToken = default) 
            => RunAsync(command, null, cancellationToken);

        public virtual Task<CmdResult> RunAsync(
            CliString command,
            string? standardInput,
            CancellationToken cancellationToken = default) 
            => base.RunRawAsync(command, standardInput, cancellationToken);

        protected override CliString TransformArguments(CliString arguments)
            => base.TransformArguments(GetPrefix() + arguments);
    }
}
