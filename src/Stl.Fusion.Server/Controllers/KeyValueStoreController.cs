using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Server.Controllers
{
    [Route("fusion/kvs/[action]")]
    [ApiController, JsonifyErrors(RewriteErrors = true)]
    public class SandboxedKeyValueStoreController : ControllerBase, ISandboxedKeyValueStore
    {
        protected ISandboxedKeyValueStore Store { get; }
        protected ISessionResolver SessionResolver { get; }

        public SandboxedKeyValueStoreController(
            ISandboxedKeyValueStore store,
            ISessionResolver sessionResolver)
        {
            Store = store;
            SessionResolver = sessionResolver;
        }

        // Commands

        [HttpPost]
        public Task Set([FromBody] SandboxedSetCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Store.Set(command, cancellationToken);
        }

        [HttpPost]
        public Task SetMany([FromBody] SandboxedSetManyCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Store.SetMany(command, cancellationToken);
        }

        [HttpPost]
        public Task Remove([FromBody] SandboxedRemoveCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Store.Remove(command, cancellationToken);
        }

        [HttpPost]
        public Task RemoveMany([FromBody] SandboxedRemoveManyCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Store.RemoveMany(command, cancellationToken);
        }

        // Queries

        [HttpGet, Publish]
        public Task<string?> TryGet(Session? session, string key, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return Store.TryGet(session, key, cancellationToken);
        }

        [HttpGet, Publish]
        public Task<int> Count(Session? session, string prefix, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return Store.Count(session, prefix, cancellationToken);
        }

        [HttpGet, Publish]
        public Task<string[]> ListKeySuffixes(
            Session? session,
            string prefix,
            PageRef<string> pageRef,
            SortDirection sortDirection = SortDirection.Ascending,
            CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return Store.ListKeySuffixes(session, prefix, pageRef, sortDirection, cancellationToken);
        }
    }
}
