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
        public static readonly CliString DefaultPrefix = CliString.New("-c").VaryByOS("/C");

        public Shell(
            CliString? executable = null, 
            CliString? argumentsPrefix = null, 
            ICliFormatter cliFormatter = null) 
            : base(
                executable ?? DefaultExecutable, 
                argumentsPrefix ?? DefaultPrefix, 
                cliFormatter) { }

        public virtual Task<ExecutionResult> RunAsync(
            CliString command, 
            CancellationToken cancellationToken = default) 
            => base.RunRawAsync(command.Quote(), cancellationToken);

        public virtual async Task<string> GetOutputAsync(
            CliString command, 
            CancellationToken cancellationToken = default) 
            => (await RunAsync(command, cancellationToken)).StandardOutput;
    }
}
