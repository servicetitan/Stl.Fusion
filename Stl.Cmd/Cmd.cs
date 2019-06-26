using System.Diagnostics;

namespace Stl.Cmd 
{
    public static class Cmd
    {
        public static RawCmdBuilder<RawCmdResult> Raw(CmdPart executable, CmdPart arguments)
            => Raw<RawCmdResult>(executable, arguments);

        public static RawCmdBuilder<TResult> Raw<TResult>(CmdPart executable, CmdPart arguments)
            where TResult : CmdResult
            => new RawCmdBuilder<TResult>(executable, arguments);
    }
}
