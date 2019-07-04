using Stl.ParametersSerializer;

namespace Stl.Terraform.Parameters
{
    public class FmtParameters
    {
        /// <summary>  
        /// Don't list the files containing formatting inconsistencies.
        /// </summary>
        [CliParameter("-list={value}")]
        public BoolEnum? List { get; set; }
        
        /// <summary>  
        /// Don't overwrite the input files. (This is implied by -check or when the input is STDIN.)
        /// </summary>
        [CliParameter("-write={value}")]
        public BoolEnum? Write { get; set; }
        
        /// <summary>  
        /// Display diffs of formatting changes
        /// </summary>
        [CliParameter("-diff")]
        public bool? Diff { get; set; }
        
        /// <summary>  
        /// Check if the input is formatted. Exit status will be 0 if all input is properly formatted and non-zero otherwise.
        /// </summary>
        [CliParameter("-check")]
        public bool? Check { get; set; }
    }
}