using Stl.OS;
using ShellCmd=Stl.CommandLine.Shell;

namespace Stl.CommandLine
{
    public static class CmdBuilders
    {
        private static CliString QuoteIfNonWindows(CliString source)
            => OSInfo.Kind != OSKind.Windows ? source.Quote() : source;

        public static CliString GetShellArguments(CliString command, CliString? prefix = default)
            => (prefix ?? Shell.DefaultPrefix) + QuoteIfNonWindows(command); 

        public static CliString GetEchoArguments(CliString source)
        {
            var result = source.Quote().Value;
            if (OSInfo.Kind != OSKind.Windows || result.Length < 2)
                return result;
            return result.Substring(1, result.Length - 2);
        }
    }
}
