using System;

namespace Stl.CommandLine
{
    [Serializable]
    public class ShellCmd : ShellLikeCmdBase
    {
        public static readonly CliString DefaultExecutable = CliString.New("bash").VaryByOS("cmd.exe");
        public static readonly CliString DefaultPrefix = CliString.New("-c").VaryByOS("/C");

        public CliString Prefix { get; set; }
        
        public ShellCmd(CliString? executable = null)
            : base(executable ?? DefaultExecutable) 
            => Prefix = DefaultPrefix;

        protected override CliString GetPrefix() => Prefix;

        protected override CliString TransformArguments(CliString arguments)
            => CmdHelpers.GetShellArguments(arguments, Prefix);
    }
}
