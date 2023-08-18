namespace Stl.Net;

public interface IRetryDelayer
{
    IMomentClock Clock { get; }
    CancellationToken CancelDelaysToken { get; }

    RetryDelay GetDelay(int tryIndex, CancellationToken cancellationToken = default);
    void CancelDelays();
}

public interface IRetryDelayer<TConsumer> : IRetryDelayer
{ }
