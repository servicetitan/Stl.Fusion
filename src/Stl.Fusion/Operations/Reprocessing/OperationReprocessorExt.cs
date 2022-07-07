namespace Stl.Fusion.Operations.Reprocessing;

public static class OperationReprocessorExt
{
    public static bool IsTransientFailure(this IOperationReprocessor reprocessor, Exception error)
        => reprocessor.IsTransientFailure(error.Flatten());

    public static bool WillRetry(this IOperationReprocessor reprocessor, Exception error)
        => reprocessor.WillRetry(error.Flatten());
}
