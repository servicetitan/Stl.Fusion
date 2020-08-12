using System.Runtime.CompilerServices;

namespace Stl.Fusion.Internal
{
    public static class ComputedEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TryAddUsed(this IComputed? usedBy, IComputed? used)
        {
            if (usedBy == null || used == null)
                return;
            ((IComputedImpl) usedBy).AddUsed((IComputedImpl) used);
        }
    }
}
