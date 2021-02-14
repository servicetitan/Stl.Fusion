using System;
using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Authentication;
using Stl.Fusion.Client;
using Stl.Fusion.Extensions;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.UI.Services
{
    [RestEaseReplicaService(typeof(ITodoService), Scope = Program.ClientSideScope)]
    [BasePath("todo")]
    public interface ITodoClient
    {
        [Post("addOrUpdate")]
        Task<Todo> AddOrUpdateAsync([Body] AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default);
        [Post("remove")]
        Task RemoveAsync([Body] RemoveTodoCommand command, CancellationToken cancellationToken = default);

        [Get("find")]
        Task<Todo?> FindAsync(Session session, string id, CancellationToken cancellationToken = default);
        [Get("list")]
        Task<Todo[]> ListAsync(Session session, PageRef<string> pageRef, CancellationToken cancellationToken = default);
    }
}
