using Stl.CommandLine;

namespace Stl.Terraform 
{
    public abstract class ApplyLikeArgumentsBase : TerraformArgumentsBase
    {
        /// <summary>  
        /// Lock the state file when locking is supported.
        /// </summary>
        [CliArgument("-lock={0}")]
        public bool? Lock { get; set; }
        
        /// <summary>  
        /// Duration to retry a state lock.
        /// </summary>
        [CliArgument("-lock-timeout={0}s")]
        public int? LockTimeout { get; set; }

        /// <summary>  
        /// Limit the number of concurrent operation as Terraform walks the graph. Defaults to 10.
        /// </summary>
        [CliArgument("-parallelism={0}")]
        public int? Parallelism { get; set; }
        
        /// <summary>  
        /// Update the state for each resource prior to planning and applying.
        /// This has no effect if a plan file is given directly to apply.
        /// </summary>
        [CliArgument("-refresh={0}")]
        public bool? Refresh { get; set; }
        
        /// <summary>  
        /// Path to the state file. Defaults to "terraform.tfstate".
        /// Ignored when remote state is used.
        /// </summary>
        [CliArgument("-state={0:Q}")]
        public bool? State { get; set; }
        
        /// <summary>  
        /// A Resource Address to target.
        /// For more information, see the targeting docs from terraform plan.
        /// </summary>
        [CliArgument("-target={0:Q}")]
        public CliString Target { get; set; }
    }
}
