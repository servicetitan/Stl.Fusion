using System.Collections.Generic;
using Stl.ParametersSerializer;

namespace Stl.Terraform.Parameters
{
    public class ApplyParameters : IParameters
    {
        /// <summary>  
        /// Path to the backup file. Defaults to -state-out with the ".backup" extension. Disabled by setting to "-".
        /// </summary>
        [CliParameter("-backup={value}")]
        public string? Backup { get; set; }
        
        /// <summary>  
        /// Lock the state file when locking is supported.
        /// </summary>
        [CliParameter("-lock={value}")]
        public BoolEnum? Lock { get; set; }
        
        /// <summary>  
        /// Duration to retry a state lock.
        /// </summary>
        [CliParameter("-lock-timeout={value}s")]
        public int? LockTimeout { get; set; }
        
        /// <summary>  
        /// Ask for input for variables if not directly set.
        /// </summary>
        [CliParameter("-input={value}")]
        public BoolEnum? Input { get; set; }
        

        /// <summary>  
        /// Skip interactive approval of plan before applying.
        /// </summary>
        [CliParameter("-auto-approve")]
        public bool AutoApprove { get; set; }
        
        /// <summary>  
        /// Limit the number of concurrent operation as Terraform walks the graph. Defaults to 10.
        /// </summary>
        [CliParameter("-parallelism={value}")]
        public int? Parallelism { get; set; }
        
        /// <summary>  
        /// Update the state for each resource prior to planning and applying. This has no effect if a plan file is given directly to apply.
        /// </summary>
        [CliParameter("-refresh={value}")]
        public BoolEnum? Refresh { get; set; }
        
        /// <summary>  
        /// Path to the state file. Defaults to "terraform.tfstate". Ignored when remote state is used.
        /// </summary>
        [CliParameter("-state={value}")]
        public BoolEnum? State { get; set; }
        
        /// <summary>  
        /// Path to write updated state file. By default, the -state path will be used. Ignored when remote state is used.
        /// </summary>
        [CliParameter("-state-out={value}")]
        public string? StateOut { get; set; }
        
        /// <summary>  
        /// A Resource Address to target. For more information, see the targeting docs from terraform plan.
        /// </summary>
        [CliParameter("-target={value}")]
        public string? Target { get; set; }

        /// <summary>  
        /// Set a variable in the Terraform configuration. This flag can be set multiple times. Variable values are interpreted as HCL, so list and map values can be specified via this flag.
        /// </summary>
        [CliParameter("-var {value}", RepeatPattern = "{key}={value}", Separator = " ")]
        public IDictionary<string, string>? Variable { get; set; }
        
        
        /// <summary>  
        /// Set variables in the Terraform configuration from a variable file.
        /// If a terraform.tfvars or any .auto.tfvars files are present in the current directory,
        /// they will be automatically loaded. terraform.tfvars is loaded first and
        /// the .auto.tfvars files after in alphabetical order.
        /// Any files specified by -var-file override any values set automatically from files in the working directory.
        /// This flag can be used multiple times.
        /// </summary>
        [CliParameter("-var-file={value}")]
        public string? VarFile { get; set; }
    }
}