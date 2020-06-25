using Stl.CommandLine;

namespace Stl.CommandLine.Terraform
{
    public class FmtArguments
    {
        /// <summary>  
        /// Don't list the files containing formatting inconsistencies.
        /// </summary>
        [CliArgument("-list={Value}", DefaultValue = "false")]
        public bool List { get; set; }
        
        /// <summary>  
        /// Don't overwrite the input files. (This is implied by -check or when the input is STDIN.)
        /// </summary>
        [CliArgument("-write={value}", DefaultValue = "false")]
        public bool Write { get; set; }
        
        /// <summary>  
        /// Display diffs of formatting changes
        /// </summary>
        [CliArgument("-diff", DefaultValue = "false")]
        public bool Diff { get; set; }
        
        /// <summary>  
        /// Check if the input is formatted. Exit status will be 0 if all input is properly formatted and non-zero otherwise.
        /// </summary>
        [CliArgument("-check", DefaultValue = "false")]
        public bool Check { get; set; }

        [CliArgument("-no-color", DefaultValue = "false")]
        public bool NoColor { get; set; }

        [CliArgument("-recursive", DefaultValue = "false")]
        public bool Recursive { get; set; }
    }
}
