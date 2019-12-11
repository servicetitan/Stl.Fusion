using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;

namespace Stl.CommandLine 
{
    public class ProxyCmd<TTargetCmd> : CmdBase
        where TTargetCmd : ICmd
    {
        public Func<TTargetCmd> TargetCmdFactory { get; }

        public ProxyCmd(Func<TTargetCmd> targetCmdFactory) 
            => TargetCmdFactory = targetCmdFactory;

        protected override Task<ExecutionResult> RunRawAsyncImpl(
            CliString arguments, string? standardInput, 
            CancellationToken cancellationToken)
        {
            var targetCmd = TargetCmdFactory.Invoke();
            targetCmd.EchoMode = EchoMode;
            targetCmd.ResultChecks = ResultChecks;
            return targetCmd.RunRawAsync(arguments, standardInput, cancellationToken);
        }
    }
}
