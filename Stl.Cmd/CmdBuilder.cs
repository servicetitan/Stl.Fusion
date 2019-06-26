using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;

namespace Stl.Cmd
{
    public abstract class CmdBuilder : CmdPart
    {
        public virtual async Task<CmdResult> RunUntypedAsync(CancellationToken cancellationToken = default)
        {
            var result = await RunInternalAsync(cancellationToken);
            return WrapResult(result);
        }

        protected virtual async Task<ExecutionResult> RunInternalAsync(CancellationToken cancellationToken = default)
        {
            var executable = GetExecutable();
            var cli = Cli.Wrap((string) executable);
            cli = ConfigureCli(cli, cancellationToken);
            return await cli.ExecuteAsync();
        }


        public abstract CmdPart GetExecutable();
        public abstract CmdPart GetArguments();
        public virtual Stream? GetStdin() => null;

        protected virtual ICli ConfigureCli(ICli cli, CancellationToken cancellationToken)
        {
            var arguments = GetArguments();
            var input = GetStdin();

            cli = cli.SetCancellationToken(cancellationToken);
            if (arguments != null)
                cli = cli.SetArguments((string) arguments);
            if (input != null)
                cli = cli.SetStandardInput(input);

            return cli;
        }

        protected virtual CmdResult WrapResult(ExecutionResult result) 
            => new RawCmdResult(this, result);

        public override string Render() => (string) (GetExecutable() + GetArguments());
    }
    
    public abstract class CmdBuilder<TResult> : CmdBuilder
        where TResult : CmdResult
    {
        public async Task<TResult> RunAsync(CancellationToken cancellationToken = default) 
            => (TResult) (await RunUntypedAsync(cancellationToken));
    }

    public sealed class RawCmdBuilder<TResult> : CmdBuilder<TResult>
        where TResult : CmdResult
    {
        public CmdPart Executable { get; }
        public CmdPart Arguments { get; }

        public RawCmdBuilder(CmdPart executable, CmdPart arguments)
        {
            Executable = executable;
            Arguments = arguments;
        }

        public override CmdPart GetExecutable() => Executable;
        public override CmdPart GetArguments() => Arguments;
    }
}
