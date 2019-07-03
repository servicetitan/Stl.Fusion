using Stl.ParametersSerializer;

namespace Stl.Terraform.Parameters
{
    public enum BoolEnum
    {
        [CliValue("true")]
        True,
        [CliValue("false")]
        False,    
    }
}