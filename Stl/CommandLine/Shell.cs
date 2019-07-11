using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;

namespace Stl.CommandLine
{
    [Serializable]
    public class Shell : Cmd
    {
        public static readonly CliString DefaultExecutable = CliString.New("bash").VaryByOS("cmd.exe");
        public static readonly CliString DefaultArgumentPrefix = CliString.New("-c").VaryByOS("/C");

        public Shell(CliString? executable = null) 
            : base(executable ?? DefaultExecutable)
        {
            ArgumentsPrefix = DefaultArgumentPrefix;
        }

        public virtual Task<ExecutionResult> RunAsync(
            CliString command, 
            CancellationToken cancellationToken = default) 
            => base.RunRawAsync(command.Quote(), null, cancellationToken);

        public virtual async Task<string> GetOutputAsync(
            CliString command, 
            CancellationToken cancellationToken = default) 
            => (await RunAsync(command, cancellationToken).ConfigureAwait(false))
                .StandardOutput;
    }
}
