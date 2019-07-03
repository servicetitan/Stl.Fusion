using System.Collections.Generic;

namespace Stl.ParametersSerializer
{
    public interface IParameterSerializer
    {
        // AY: I created CmdPart class to encapsulate part serialization,
        // i.e. it makes sense to either turn this into IEnumerable<CmdPart>
        // (which by itself doesn't add much extra value in comparison to IEnumerable<CmdPart>),
        // or talk with me on whether CmdPart is a good idea in general.
        // In short, if you declare this, it breaks consistency.
        IEnumerable<string> Serialize(IParameters parameters);
    }
}
