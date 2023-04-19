using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication;

public interface ISessionResolver : IHasServices
{
    public Task<Session> SessionTask { get; }
    public bool HasSession { get; }
    public Session Session { get; }
    public Task<Session> GetSession(CancellationToken cancellationToken = default);
}

public interface ISessionProvider : ISessionResolver
{
    public new Session Session { get; set; }
}

public class SessionProvider : ISessionProvider
{
    protected readonly TaskCompletionSource<Session> SessionSource = TaskCompletionSourceExt.New<Session>();

    public IServiceProvider Services { get; }
    public Task<Session> SessionTask => SessionSource.Task;
    public bool HasSession => SessionTask.IsCompleted;
    public Session Session {
        get => HasSession ? SessionTask.Result : throw Errors.NoSessionProvided();
        set {
            if (!Services.IsScoped())
                throw Errors.SessionProviderSessionCannotBeSetForRootInstance();
            SessionSource.TrySetResult(value.Require());
        }
    }

    public SessionProvider(IServiceProvider services) 
        => Services = services;

    public virtual Task<Session> GetSession(CancellationToken cancellationToken = default)
        => SessionTask.WaitAsync(cancellationToken);
}
