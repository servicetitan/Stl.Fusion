namespace Stl.Fusion.Internal;

public static class ComputedImplExt
{
    public static void CopyAllUsedTo(this IComputedImpl computed, ref ArrayBuffer<IComputedImpl> buffer)
    {
        var startCount = buffer.Count;
        computed.CopyUsedTo(ref buffer);
        var endCount = buffer.Count;
        for (var i = startCount; i < endCount; i++) {
            var c = buffer[i];
            c.CopyUsedTo(ref buffer);
        }
    }
}
