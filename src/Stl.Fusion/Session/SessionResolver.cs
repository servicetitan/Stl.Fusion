using Stl.Fusion.Internal;

namespace Stl.Fusion;

#pragma warning disable VSTHRD104

public interface ISessionResolver : IHasServices
{
    public Task<Session> SessionTask { get; }
    public bool HasSession { get; }
#pragma warning disable CA1721
    public Session Session { get; set; }
#pragma warning restore CA1721

    public Task<Session> GetSession(CancellationToken cancellationToken = default);
}

public class SessionResolver(IServiceProvider services) : ISessionResolver
{
    protected readonly TaskCompletionSource<Session> SessionSource = TaskCompletionSourceExt.New<Session>();

    public IServiceProvider Services { get; } = services;
    public Task<Session> SessionTask => SessionSource.Task;
    public bool HasSession => SessionTask.IsCompleted;

#pragma warning disable CA1721
    public Session Session {
#pragma warning restore CA1721
        get => HasSession ? SessionTask.Result : throw Stl.Internal.Errors.NotInitialized(nameof(Session));
        set {
            if (!Services.IsScoped())
                throw Errors.SessionResolverSessionCannotBeSetForRootInstance();

            SessionSource.TrySetResult(value.Require());
        }
    }

    public virtual Task<Session> GetSession(CancellationToken cancellationToken = default)
        => SessionTask.WaitAsync(cancellationToken);
}
