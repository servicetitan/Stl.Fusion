using Stl.CommandLine;

namespace Stl.Terraform 
{
    public class ImportArguments : ApplyLikeArgumentsBase
    {
        /// <summary>  
        /// Path to the backup file. Defaults to -state-out with the ".backup" extension.
        /// Disabled by setting to "-".
        /// </summary>
        [CliArgument("-backup={0:Q}")]
        public CliString Backup { get; set; }

        /// <summary>  
        /// Provider to use for import.
        /// </summary>
        [CliArgument("-provider={0:Q}")]
        public CliString Provider { get; set; }
        
        /// <summary>  
        /// Path to write updated state file. By default, the -state path will be used.
        /// Ignored when remote state is used.
        /// </summary>
        [CliArgument("-state-out={0:Q}")]
        public CliString StateOut { get; set; }

        // Address to import the resource to.
        [CliArgument("{0:Q}")]
        public CliString Address { get; set; }

        // The provider-specific ID of the resource to import.
        [CliArgument("{0:Q}")]
        public CliString Id { get; set; }
    }
}
