namespace Stl;

public static class TransientErrorDetectorExt
{
    public static ITransientErrorDetector<TContext> For<TContext>(this ITransientErrorDetector detector)
        => new CastingTransientErrorDetector<TContext>(detector);

    // Nested types

    private sealed record CastingTransientErrorDetector<TContext>(ITransientErrorDetector BaseDetector)
        : TransientErrorDetector, ITransientErrorDetector<TContext>
    {
        public override bool IsTransient(Exception error)
            => BaseDetector.IsTransient(error);
    }
}
