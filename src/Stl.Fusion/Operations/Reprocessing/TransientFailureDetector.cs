using Stl.Fusion.Operations.Reprocessing.Internal;

namespace Stl.Fusion.Operations.Reprocessing;

public interface ITransientFailureDetector
{
    bool IsTransient(Exception error);
}

public abstract class TransientFailureDetector : ITransientFailureDetector
{
    public static ITransientFailureDetector New(Func<Exception, bool> detector)
        => new FuncTransientFailureDetector(detector);

    public abstract bool IsTransient(Exception error);
}
