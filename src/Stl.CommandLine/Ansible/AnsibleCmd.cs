using System;
using Stl.IO;

namespace Stl.CommandLine.Ansible
{
    [Serializable]
    public class AnsibleCmd : ShellLikeCmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("python3" + CmdHelpers.ExeExtension);
        public static readonly PathString DefaultAnsiblePath = "ansible";

        public PathString AnsiblePath { get; set; }
        
        public AnsibleCmd(
            PathString? ansiblePath = null,
            PathString? executable = null) : base(executable ?? DefaultExecutable) 
            => AnsiblePath = ansiblePath ?? DefaultAnsiblePath;

        protected override CliString GetPrefix() => DefaultAnsiblePath;
    }
}
