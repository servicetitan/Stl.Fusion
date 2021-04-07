using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.Services
{
    public class TodoService : ITodoService
    {
        private readonly IKeyValueStore _keyValueStore;
        private readonly IAuthService _authService;

        public TodoService(IKeyValueStore keyValueStore, IAuthService authService)
        {
            _keyValueStore = keyValueStore;
            _authService = authService;
        }

        // Commands

        public virtual async Task<Todo> AddOrUpdate(AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default)
        {
            if (Computed.IsInvalidating()) return default!;
            var (session, todo) = command;
            var user = await _authService.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            Todo? oldTodo = null;
            if (string.IsNullOrEmpty(todo.Id))
                todo = todo with { Id = Ulid.NewUlid().ToString() };
            else
                oldTodo = await TryGet(session, todo.Id, cancellationToken);

            var key = GetTodoKey(user, todo.Id);
            await _keyValueStore.Set(key, todo, cancellationToken);
            if (oldTodo?.IsDone != todo.IsDone) {
                var doneKey = GetDoneKey(user, todo.Id);
                if (todo.IsDone)
                    await _keyValueStore.Set(doneKey, true, cancellationToken);
                else
                    await _keyValueStore.Remove(doneKey, cancellationToken);
            }
            return todo;
        }

        public virtual async Task Remove(RemoveTodoCommand command, CancellationToken cancellationToken = default)
        {
            if (Computed.IsInvalidating()) return;
            var (session, id) = command;
            var user = await _authService.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            var key = GetTodoKey(user, id);
            var doneKey = GetDoneKey(user, id);
            await _keyValueStore.Remove(key, cancellationToken);
            await _keyValueStore.Remove(doneKey, cancellationToken);
        }

        // Queries

        public virtual async Task<Todo?> TryGet(Session session, string id, CancellationToken cancellationToken = default)
        {
            var user = await _authService.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            var key = GetTodoKey(user, id);
            var todoOpt = await _keyValueStore.TryGet<Todo>(key, cancellationToken);
            return todoOpt.IsSome(out var todo) ? todo : null;
        }

        public virtual async Task<Todo[]> List(Session session, PageRef<string> pageRef, CancellationToken cancellationToken = default)
        {
            var user = await _authService.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            var keyPrefix = GetTodoKeyPrefix(user);
            var keySuffixes = await _keyValueStore.ListKeySuffixes(keyPrefix, pageRef, cancellationToken);
            var tasks = keySuffixes.Select(suffix => _keyValueStore.TryGet<Todo>(keyPrefix + suffix, cancellationToken));
            var todoOpts = await Task.WhenAll(tasks);
            return todoOpts.Where(todo => todo.HasValue).Select(todo => todo.Value).ToArray();
        }

        public virtual async Task<TodoSummary> GetSummary(Session session, CancellationToken cancellationToken = default)
        {
            var user = await _authService.GetUser(session, cancellationToken);
            user.MustBeAuthenticated();

            var count = await _keyValueStore.Count(GetTodoKeyPrefix(user), cancellationToken);
            var doneCount = await _keyValueStore.Count(GetDoneKeyPrefix(user), cancellationToken);
            return new TodoSummary(count, doneCount);
        }

        // Private methods

        private string GetTodoKey(User user, string id)
            => $"{GetTodoKeyPrefix(user)}/{id}";
        private string GetDoneKey(User user, string id)
            => $"{GetDoneKeyPrefix(user)}/{id}";

        private string GetTodoKeyPrefix(User user)
            => $"todo/{user.Id}/items";
        private string GetDoneKeyPrefix(User user)
            => $"todo/{user.Id}/done";
    }
}
