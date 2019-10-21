using Stl.OS;

namespace Stl.CommandLine
{
    public static class CmdHelpers
    {
        public static readonly string ExeExtension = CliString.Empty.VaryByOS(".exe").Value;
        public static readonly string ExeScriptExtension = CliString.Empty.VaryByOS(".bat").Value;
        public static readonly string ScriptExtension = new CliString(".sh").VaryByOS(".bat").Value;
        
        private static CliString QuoteIfNonWindows(CliString source)
            => OSInfo.Kind != OSKind.Windows ? source.Quote() : source;

        public static CliString GetShellArguments(CliString command, CliString? prefix = default)
            => (prefix ?? ShellCmd.DefaultPrefix) + QuoteIfNonWindows(command); 

        public static CliString GetEchoArguments(CliString source)
        {
            var result = source.Quote().Value;
            if (OSInfo.Kind != OSKind.Windows || result.Length < 2)
                return result;
            return result.Substring(1, result.Length - 2);
        }
    }
}
