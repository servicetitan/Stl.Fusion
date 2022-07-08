using System.Data;

namespace Stl.Fusion.EntityFramework;

public static class IsolationLevelExt
{
    public static IsolationLevel Max(this IsolationLevel first, IsolationLevel second)
    {
        // If one is unspecified, we return the other one
        if (second == IsolationLevel.Unspecified)
            return first;
        if (first == IsolationLevel.Unspecified)
            return second;

        // Serializable is < Snapshot somehow in these enums
        if (first == IsolationLevel.Serializable)
            return first;
        if (second == IsolationLevel.Serializable)
            return second;

        // Otherwise we return max. of two
        return (IsolationLevel) MathExt.Max((int) first, (int) second);
    }
}
