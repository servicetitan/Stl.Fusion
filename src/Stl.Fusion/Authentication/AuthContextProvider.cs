using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion.Authentication
{
    public class AuthContextProvider
    {
        protected TaskSource<AuthContext> ContextTaskSource => TaskSource.For(ContextTask);
        public Task<AuthContext> ContextTask { get; } = TaskSource.New<AuthContext>(true).Task;

        public virtual Task<AuthContext> GetContextAsync(CancellationToken cancellationToken = default)
            => ContextTask.WithFakeCancellation(cancellationToken);

        public virtual void SetContext(AuthContext context)
        {
            context.AssertNotNull();
            ContextTaskSource.SetResult(context);
        }

        public virtual void TrySetContext(AuthContext context)
        {
            context.AssertNotNull();
            ContextTaskSource.TrySetResult(context);
        }
    }
}
