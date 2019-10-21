using Stl.CommandLine;

namespace Stl.CommandLine.Terraform 
{
    public class ImportArguments : ApplyLikeArgumentsBase
    {
        /// <summary>  
        /// Provider to use for import.
        /// </summary>
        [CliArgument("-provider={0:Q}")]
        public CliString Provider { get; set; }
        
        // Address to import the resource to.
        [CliArgument("{0:Q}")]
        public CliString ResourceAddress { get; set; }

        // The provider-specific ID of the resource to import.
        [CliArgument("{0:Q}")]
        public CliString ResourceId { get; set; }
    }
}
