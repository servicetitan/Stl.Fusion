using System;
using System.IO;
using System.Text.RegularExpressions;
using Stl.IO;

namespace Stl.CommandLine 
{
    public static class CmdEx
    {
        public static Disposable<(ICmd, CmdResultChecks)> ChangeResultChecks(
            this ICmd cmd, CmdResultChecks newResultChecks)
        {
            var oldValue = cmd.ResultChecks;
            cmd.ResultChecks = newResultChecks;
            return Disposable.New(
                state => state.Cmd.ResultChecks = state.OldValue, 
                (Cmd: cmd, OldValue: oldValue));
        }
    }
}
