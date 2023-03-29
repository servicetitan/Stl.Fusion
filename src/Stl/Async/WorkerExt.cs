namespace Stl.Async;

public static class WorkerExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TWorker Start<TWorker>(this TWorker worker)
        where TWorker : IWorker
    {
        worker.Run();
        return worker;
    }
}
