using System.Collections.Generic;

namespace Stl.ParametersSerializer
{
    public interface IParameterSerializer
    {
        IEnumerable<string> Serialize(IParameters parameters);
    }
}