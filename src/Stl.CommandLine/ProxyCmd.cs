using System;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;

namespace Stl.CommandLine 
{
    public class ProxyCmd<TTargetCmd> : CmdBase
        where TTargetCmd : ICmd
    {
        public Func<TTargetCmd> TargetCmdFactory { get; }

        public ProxyCmd(Func<TTargetCmd> targetCmdFactory) 
            => TargetCmdFactory = targetCmdFactory;

        protected override Task<CmdResult> RunRawAsyncImpl(
            CliString arguments, string? standardInput, 
            CancellationToken cancellationToken)
        {
            var targetCmd = TargetCmdFactory.Invoke();
            targetCmd.EchoMode = EchoMode;
            targetCmd.ResultValidation = ResultValidation;
            return targetCmd.RunRawAsync(arguments, standardInput, cancellationToken);
        }
    }
}
