using System;
using System.IO;
using CliWrap;

namespace Build
{
    internal static class CliWrapCommandExtensions
    {
        internal static Stream _stdout = Console.OpenStandardOutput();
        internal static Stream _stderr = Console.OpenStandardError();

        internal static Command ToConsole(this Command command) => command | (_stdout, _stderr);
    }
}

