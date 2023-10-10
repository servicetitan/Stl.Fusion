using Stl.DependencyInjection;
using Stl.Fusion.Extensions;
using Templates.TodoApp.Abstractions;

namespace Templates.TodoApp.Services;

public class TodoUI(Session session, ITodos todos) : IComputeService, IDisposable, IHasIsDisposed
{
    private volatile int _isDisposed;

    public Session Session { get; } = session;
    public bool IsDisposed => _isDisposed != 0;

    public void Dispose()
        => Interlocked.Exchange(ref _isDisposed, 1);

    [ComputeMethod]
    public virtual Task<Todo?> Get(string id, CancellationToken cancellationToken = default)
        => todos.Get(Session, id, cancellationToken);

    [ComputeMethod]
    public virtual Task<Todo[]> List(PageRef<string> pageRef, CancellationToken cancellationToken = default)
        => todos.List(Session, pageRef, cancellationToken);

    [ComputeMethod]
    public virtual Task<TodoSummary> GetSummary(CancellationToken cancellationToken = default)
        => todos.GetSummary(Session, cancellationToken);
}
