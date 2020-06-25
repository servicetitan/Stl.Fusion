using System;

namespace Stl.CommandLine
{
    [Flags]
    public enum CmdResultChecks
    {
        NonZeroExitCode = 1,
        NonEmptyStandardError = 2,
    }
}
