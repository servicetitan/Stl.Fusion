using Stl.CommandLine;

namespace Stl.CommandLine.Terraform 
{
    public class PlanArguments : PlanLikeArgumentsBase
    {
        /// <summary>  
        /// The path to save the generated execution plan.
        /// This plan can then be used with terraform apply to be certain that only
        /// the changes shown in this plan are applied. Read the warning on saved plans below.
        /// </summary>
        [CliArgument("-out={0:Q}")]
        public CliString Out { get; set; }
    }
}
