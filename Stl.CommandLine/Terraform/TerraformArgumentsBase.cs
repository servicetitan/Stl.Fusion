using Stl.CommandLine;

namespace Stl.CommandLine.Terraform 
{
    public abstract class TerraformArgumentsBase
    {
        [CliArgument("-no-color", DefaultValue = "false")]
        public bool NoColor { get; set; }

        /// <summary>  
        /// Ask for input for variables if not directly set.
        /// </summary>
        [CliArgument("-input={0}", DefaultValue = "true")]
        public bool Input { get; set; } = true;
        
        /// <summary>  
        /// Set a variable in the Terraform configuration.
        /// This flag can be set multiple times. Variable values are interpreted as HCL,
        /// so list and map values can be specified via this flag.
        /// </summary>
        [CliArgument("-var {0:Q}")]
        public CliDictionary<CliString, CliString> Variables { get; set; } = 
            new CliDictionary<CliString, CliString>();

        /// <summary>  
        /// Set variables in the Terraform configuration from a variable file.
        /// If a terraform.tfvars or any .auto.tfvars files are present in the current directory,
        /// they will be automatically loaded. terraform.tfvars is loaded first and
        /// the .auto.tfvars files after in alphabetical order.
        /// Any files specified by -var-file override any values set automatically from files
        /// in the working directory.
        /// This flag can be used multiple times.
        /// </summary>
        [CliArgument("-var-file={0:Q}")]
        public CliString VarFile { get; set; }
    }
}
