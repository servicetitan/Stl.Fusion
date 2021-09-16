using System;
using System.IO;
using CliWrap;
using CliWrap.Builders;

namespace Build
{
    internal static class CliWrapCommandExt
    {
        private static readonly Stream StdOut = Console.OpenStandardOutput();
        private static readonly Stream StdErr = Console.OpenStandardError();

        internal static Command ToConsole(this Command command) => command | (StdOut, StdErr);

        internal static ArgumentsBuilder AddOption(this ArgumentsBuilder args, string name, string value) =>
            !string.IsNullOrEmpty(value)
                ? args.Add(name).Add(value)
                : args;
    }
}
