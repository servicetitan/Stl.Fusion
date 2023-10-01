namespace Stl.Fusion.Client.Interception;

internal static class AlreadySynchronized
{
    public static readonly TaskCompletionSource<Unit> Source
        = TaskCompletionSourceExt.New<Unit>().WithResult(default);
}
