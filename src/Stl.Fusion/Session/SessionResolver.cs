using Stl.Fusion.Internal;

namespace Stl.Fusion;

#pragma warning disable VSTHRD104

public interface ISessionResolver : IHasServices
{
    public Task<Session> SessionTask { get; }
    public bool HasSession { get; }
    public Session Session { get; set; }

    public Task<Session> GetSession(CancellationToken cancellationToken = default);
}

public class SessionResolver : ISessionResolver
{
    protected readonly TaskCompletionSource<Session> SessionSource = TaskCompletionSourceExt.New<Session>();

    public IServiceProvider Services { get; }
    public Task<Session> SessionTask => SessionSource.Task;
    public bool HasSession => SessionTask.IsCompleted;

    public Session Session {
        get => HasSession ? SessionTask.Result : throw Stl.Internal.Errors.NotInitialized(nameof(Session));
        set {
            if (!Services.IsScoped())
                throw Errors.SessionResolverSessionCannotBeSetForRootInstance();

            SessionSource.TrySetResult(value.Require());
        }
    }

    public SessionResolver(IServiceProvider services)
        => Services = services;

    public virtual Task<Session> GetSession(CancellationToken cancellationToken = default)
        => SessionTask.WaitAsync(cancellationToken);
}
