using System;
using Stl.IO;

namespace Stl.CommandLine
{
    [Serializable]
    public class ShellCmd : ShellLikeCmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("bash").VaryByOS("cmd.exe");
        public static readonly CliString DefaultPrefix = CliString.New("-c").VaryByOS("/C");

        public CliString Prefix { get; set; }
        
        public ShellCmd(PathString? executable = null)
            : base(executable ?? DefaultExecutable) 
            => Prefix = DefaultPrefix;

        protected override CliString GetPrefix() => Prefix;

        protected override CliString TransformArguments(CliString arguments)
        {
            // Shouldn't call base method b/c it also adds Prefix (but differently)
            var shellArguments = CmdHelpers.GetShellArguments(arguments, Prefix);
            return ArgumentTransformer?.Invoke(shellArguments) ?? shellArguments;
        }
    }
}
