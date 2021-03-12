using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.Authentication
{
    public interface ISessionResolver
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
        protected TaskSource<Session> SessionTaskSource => TaskSource.For(SessionTask);
        public Task<Session> SessionTask { get; } = TaskSource.New<Session>(true).Task;
        public bool HasSession => SessionTask.IsCompleted;
        public Session Session {
            get => HasSession ? SessionTask.Result : throw Errors.NoSessionProvided();
            set => SessionTaskSource.TrySetResult(value.AssertNotNull());
        }

        public virtual Task<Session> GetSession(CancellationToken cancellationToken = default)
            => SessionTask.WithFakeCancellation(cancellationToken);
    }
}
