using System;
using System.IO;
using System.Text.RegularExpressions;
using CliWrap;
using Stl.IO;

namespace Stl.CommandLine 
{
    public static class CmdEx
    {
        public static Disposable<(ICmd, CommandResultValidation)> ChangeResultValidation(
            this ICmd cmd, CommandResultValidation newResultValidation)
        {
            var oldValue = cmd.ResultValidation;
            cmd.ResultValidation = newResultValidation;
            return Disposable.New(
                (Cmd: cmd, OldValue: oldValue),
                state => state.Cmd.ResultValidation = state.OldValue);
        }
    }
}
