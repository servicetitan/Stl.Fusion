using System;
using System.IO;

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

        public static void SetRelativeWorkingDirectory(this ICmd cmd, string workingDirectory)
        {
            if (string.IsNullOrEmpty(workingDirectory))
                return;
            if (Path.IsPathFullyQualified(workingDirectory)) {
                cmd.WorkingDirectory = workingDirectory;
                return;
            }
            if (!string.IsNullOrEmpty(cmd.WorkingDirectory.Value)) {
                cmd.WorkingDirectory = Path.Combine(cmd.WorkingDirectory.Value, workingDirectory);
                return;
            }
            cmd.WorkingDirectory = Path.Combine(Environment.CurrentDirectory, workingDirectory);
        }
    }
}
