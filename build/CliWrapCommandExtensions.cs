using System;
using System.IO;
using CliWrap;
using CliWrap.Builders;

namespace Build
{
    internal static class CliWrapCommandExtensions
    {
        internal static Stream _stdout = Console.OpenStandardOutput();
        internal static Stream _stderr = Console.OpenStandardError();

        internal static Command ToConsole(this Command command) => command | (_stdout, _stderr);

        internal static ArgumentsBuilder AddOption(this ArgumentsBuilder args, string name, string value) =>
            !string.IsNullOrEmpty(value)
                ? args.Add(name).Add(value)
                : args;
    }
}