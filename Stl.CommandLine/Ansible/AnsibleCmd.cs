using System;

namespace Stl.CommandLine.Ansible
{
    [Serializable]
    public class AnsibleCmd : ShellLikeCmdBase
    {
        public static readonly CliString DefaultExecutable = CliString.New("python3" + CmdHelpers.ExeExtension);
        public static readonly CliString DefaultAnsiblePath = CliString.New("ansible");

        public CliString AnsiblePath { get; set; }
        
        public AnsibleCmd(
            CliString? ansiblePath = null,
            CliString? executable = null) : base(executable ?? DefaultExecutable) 
            => AnsiblePath = ansiblePath ?? DefaultAnsiblePath;

        protected override CliString GetPrefix() => DefaultAnsiblePath;
    }
}
