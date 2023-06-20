using Stl.Fusion.Extensions;
using Templates.TodoApp.Abstractions;

namespace Templates.TodoApp.Services;

#pragma warning disable 1998

public class SimpleTodos : ITodos
{
    private ImmutableList<Todo> _store = ImmutableList<Todo>.Empty; // It's always sorted by Id though

    // Commands

    public virtual async Task<Todo> AddOrUpdate(Todos_AddOrUpdate command, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating())
            return null!;

        var (session, todo) = command;
        if (string.IsNullOrEmpty(todo.Id))
            todo = todo with { Id = Ulid.NewUlid().ToString() };
        _store = _store.RemoveAll(i => i.Id == todo.Id).Add(todo);

        using var invalidating = Computed.Invalidate();
        _ = Get(session, todo.Id, default);
        _ = PseudoGetAllItems(session);
        return todo;
    }

    public virtual async Task Remove(Todos_Remove command, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating())
            return;

        var (session, todoId) = command;
        _store = _store.RemoveAll(i => i.Id == todoId);

        using var invalidating = Computed.Invalidate();
        _ = Get(session, todoId, default);
        _ = PseudoGetAllItems(session);
    }

    // Queries

    public virtual Task<Todo?> Get(Session session, string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.SingleOrDefault(i => i.Id == id));

    public virtual async Task<Todo[]> List(Session session, PageRef<string> pageRef, CancellationToken cancellationToken = default)
    {
        await PseudoGetAllItems(session);
        return _store.OrderByAndTakePage(i => i.Id, pageRef).ToArray();
    }

    public virtual async Task<TodoSummary> GetSummary(Session session, CancellationToken cancellationToken = default)
    {
        await PseudoGetAllItems(session);
        var count = _store.Count();
        var doneCount = _store.Count(i => i.IsDone);
        return new TodoSummary(count, doneCount);
    }

    // Pseudo queries

    [ComputeMethod]
    protected virtual Task<Unit> PseudoGetAllItems(Session session)
        => TaskExt.UnitTask;
}
