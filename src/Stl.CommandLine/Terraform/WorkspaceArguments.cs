namespace Stl.CommandLine.Terraform
{
    public class WorkspaceNewArguments
    {
        [CliArgument("-lock={0}", DefaultValue = "true")]
        public bool Lock { get; set; } = true;
        
        [CliArgument("-lock-timeout={0}s")]
        public int? LockTimeout { get; set; }

        [CliArgument("-state={0:Q}")]
        public CliString State { get; set; }
    }

    public class WorkspaceDeleteArguments
    {
        [CliArgument("-force", DefaultValue = "false")]
        public bool Force { get; set; } = false;
        
        [CliArgument("-lock={0}", DefaultValue = "true")]
        public bool Lock { get; set; } = true;
        
        [CliArgument("-lock-timeout={0}s")]
        public int? LockTimeout { get; set; }
    }
}
