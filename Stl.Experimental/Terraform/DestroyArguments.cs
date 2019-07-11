using Stl.CommandLine;

namespace Stl.Terraform
{
    public class DestroyArguments
    {
        /// <summary>  
        /// Skip interactive approval of plan before applying.
        /// </summary>
        [CliArgument("-auto-approve", DefaultValue = "false")]
        public CliBool AutoApprove => true;
    }
}