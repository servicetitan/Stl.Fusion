using Stl.Extensibility;

namespace Stl.Fusion.Interception;

[MatchFor(typeof(CancellationToken), typeof(ArgumentHandlerProvider))]
public class IgnoreArgumentHandler : ArgumentHandler
{
    public static IgnoreArgumentHandler Instance { get; } = new();

    private IgnoreArgumentHandler()
    {
        GetHashCodeFunc = _ => 0;
        EqualsFunc = (a, b) => true;
        ToStringFunc = _ => "";
    }
}
