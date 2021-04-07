using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.Services
{
    public class SimpleTodoService : ITodoService
    {
        private ImmutableList<Todo> _store = ImmutableList<Todo>.Empty; // It's always sorted by Id though

        // Commands

        public virtual Task<Todo> AddOrUpdate(AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default)
        {
            var (session, todo) = command;
            if (Computed.IsInvalidating()) {
                TryGet(session, todo.Id, CancellationToken.None).Ignore();
                PseudoAnyItem(session).Ignore();
                return Task.FromResult(default(Todo)!);
            }

            if (string.IsNullOrEmpty(todo.Id))
                todo = todo with { Id = Ulid.NewUlid().ToString() };
            _store = _store.RemoveAll(i => i.Id == todo.Id).Add(todo);
            return Task.FromResult(todo);
        }

        public virtual Task Remove(RemoveTodoCommand command, CancellationToken cancellationToken = default)
        {
            var (session, id) = command;
            if (Computed.IsInvalidating()) {
                TryGet(session, id, CancellationToken.None).Ignore();
                PseudoAnyItem(session).Ignore();
                return Task.CompletedTask;
            }

            _store = _store.RemoveAll(i => i.Id == id);
            return Task.CompletedTask;
        }

        // Queries

        public virtual Task<Todo?> TryGet(Session session, string id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.SingleOrDefault(i => i.Id == id));

        public virtual async Task<Todo[]> List(Session session, PageRef<string> pageRef, CancellationToken cancellationToken = default)
        {
            await PseudoAnyItem(session);
            var todos = _store.AsEnumerable();
            if (pageRef.AfterKey != null)
                todos = todos.Where(i => string.CompareOrdinal(i.Id, pageRef.AfterKey) > 0);
            todos = todos.Take(pageRef.Count);
            return todos.ToArray();
        }

        public virtual async Task<TodoSummary> GetSummary(Session session, CancellationToken cancellationToken = default)
        {
            await PseudoAnyItem(session);
            var count = _store.Count();
            var doneCount = _store.Count(i => i.IsDone);
            return new TodoSummary(count, doneCount);
        }

        // Pseudo queries

        [ComputeMethod]
        protected virtual Task<Unit> PseudoAnyItem(Session session)
            => TaskEx.UnitTask;
    }
}
