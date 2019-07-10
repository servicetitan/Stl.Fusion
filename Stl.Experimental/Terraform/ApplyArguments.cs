using Stl.CommandLine;

namespace Stl.Terraform
{
    public class ApplyArguments : ApplyLikeArgumentsBase
    {
        /// <summary>  
        /// Path to the backup file. Defaults to -state-out with the ".backup" extension.
        /// Disabled by setting to "-".
        /// </summary>
        [CliArgument("-backup={0:Q}")]
        public CliString Backup { get; set; }

        /// <summary>  
        /// Skip interactive approval of plan before applying.
        /// </summary>
        [CliArgument("-auto-approve", DefaultValue = "false")]
        public CliBool AutoApprove { get; set; }
        
        /// <summary>  
        /// Path to write updated state file. By default, the -state path will be used.
        /// Ignored when remote state is used.
        /// </summary>
        [CliArgument("-state-out={0:Q}")]
        public CliString StateOut { get; set; }
    }
}
