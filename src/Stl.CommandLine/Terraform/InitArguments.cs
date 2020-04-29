namespace Stl.CommandLine.Terraform
{
    public class InitArguments : TerraformArgumentsBase
    {
        [CliArgument("-backend={0:Q}", DefaultValue = "true")]
        public bool Backend { get; set; } = true;

        [CliArgument("-backend-config={0:Q}")]
        public CliList<CliString> BackendConfig { get; set; } = new CliList<CliString>();

        [CliArgument("-force-copy", DefaultValue = "false")]
        public bool ForceCopy { get; set; }

        [CliArgument("-from-module={0:Q}")]
        public CliList<string> FromModule { get; set; } = new CliList<string>();

        [CliArgument("-get={0}", DefaultValue = "true")]
        public bool GetModules { get; set; } = true;

        [CliArgument("-get-plugins={0}", DefaultValue = "true")]
        public bool GetPlugins { get; set; } = true;

        [CliArgument("-lock={0}", DefaultValue = "true")]
        public bool Lock { get; set; } = true;
        
        [CliArgument("-lock-timeout={0}s")]
        public int? LockTimeout { get; set; }

        [CliArgument("-plugin-dir {0:Q}")]
        public CliString PluginDir { get; set; }

        [CliArgument("-reconfigure", DefaultValue = "false")]
        public bool Reconfigure { get; set; }

        [CliArgument("-upgrade", DefaultValue = "false")]
        public bool Upgrade { get; set; }

        [CliArgument("-verify-plugins", DefaultValue = "true")]
        public bool VerifyPlugins { get; set; } = true;
    }
}
