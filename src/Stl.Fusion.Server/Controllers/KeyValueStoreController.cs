using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Extensions.Commands;
using Stl.Fusion.Extensions.Internal;

namespace Stl.Fusion.Server.Controllers
{
    [Route("fusion/kvs/[action]")]
    [ApiController, JsonifyErrors(RewriteErrors = true)]
    public class KeyValueStoreController : ControllerBase, IKeyValueStore
    {
        protected IKeyValueStore KeyValueStore { get; }
        protected IKeyValueStoreSandboxProvider KeyValueStoreSandboxProvider { get; }
        protected ISessionResolver SessionResolver { get; }

        public KeyValueStoreController(
            IKeyValueStore keyValueStore,
            IKeyValueStoreSandboxProvider keyValueStoreSandboxProvider,
            ISessionResolver sessionResolver)
        {
            KeyValueStore = keyValueStore;
            KeyValueStoreSandboxProvider = keyValueStoreSandboxProvider;
            SessionResolver = sessionResolver;
        }

        // Commands

        [HttpPost]
        public async Task Set([FromBody] SetCommand command, CancellationToken cancellationToken = default)
        {
            var session = SessionResolver.Session;
            var keySandbox = await KeyValueStoreSandboxProvider.GetSandbox(session, cancellationToken);
            command = command with {
                Key = keySandbox.Apply(command.Key),
                ExpiresAt = keySandbox.Apply(command.ExpiresAt),
                IsServerSide = true
            };
            await KeyValueStore.Set(command, cancellationToken);
        }

        [HttpPost]
        public async Task SetMany([FromBody] SetManyCommand command, CancellationToken cancellationToken = default)
        {
            var session = SessionResolver.Session;
            var keySandbox = await KeyValueStoreSandboxProvider.GetSandbox(session, cancellationToken);
            var items = command.Items;
            for (var i = 0; i < items.Length; i++) {
                var item = items[i];
                items[i] = (keySandbox.Apply(item.Key), item.Value, keySandbox.Apply(item.ExpiresAt));
            }
            command = command with {
                Items = items,
                IsServerSide = true,
            };
            await KeyValueStore.SetMany(command, cancellationToken);
        }

        [HttpPost]
        public async Task Remove([FromBody] RemoveCommand command, CancellationToken cancellationToken = default)
        {
            var session = SessionResolver.Session;
            var keySandbox = await KeyValueStoreSandboxProvider.GetSandbox(session, cancellationToken);
            command = command with {
                Key = keySandbox.Apply(command.Key),
                IsServerSide = true
            };
            await KeyValueStore.Remove(command, cancellationToken);
        }

        [HttpPost]
        public async Task RemoveMany([FromBody] RemoveManyCommand command, CancellationToken cancellationToken = default)
        {
            var session = SessionResolver.Session;
            var keySandbox = await KeyValueStoreSandboxProvider.GetSandbox(session, cancellationToken);
            var keys = command.Keys;
            for (var i = 0; i < keys.Length; i++)
                keys[i] = keySandbox.Apply(keys[i]);
            command = command with {
                Keys = keys,
                IsServerSide = true,
            };
            await KeyValueStore.RemoveMany(command, cancellationToken);
        }

        // Queries

        [HttpGet, Publish]
        public async Task<string?> TryGet(string key, CancellationToken cancellationToken = default)
        {
            var session = SessionResolver.Session;
            var keySandbox = await KeyValueStoreSandboxProvider.GetSandbox(session, cancellationToken);
            key = keySandbox.Apply(key);
            return await KeyValueStore.TryGet(key, cancellationToken);
        }

        [HttpGet, Publish]
        public async Task<int> Count(string prefix, CancellationToken cancellationToken = default)
        {
            var session = SessionResolver.Session;
            var keySandbox = await KeyValueStoreSandboxProvider.GetSandbox(session, cancellationToken);
            prefix = keySandbox.Apply(prefix);
            return await KeyValueStore.Count(prefix, cancellationToken);
        }

        [HttpGet, Publish]
        public async Task<string[]> ListKeySuffixes(
            string prefix,
            PageRef<string> pageRef,
            SortDirection sortDirection = SortDirection.Ascending,
            CancellationToken cancellationToken = default)
        {
            var session = SessionResolver.Session;
            var keySandbox = await KeyValueStoreSandboxProvider.GetSandbox(session, cancellationToken);
            prefix = keySandbox.Apply(prefix);
            if (pageRef.AfterKey != null)
                pageRef = new PageRef<string>(pageRef.Count, keySandbox.Apply(pageRef.AfterKey));
            return await KeyValueStore.ListKeySuffixes(prefix, pageRef, sortDirection, cancellationToken);
        }
    }
}
