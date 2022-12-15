using System.Globalization;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions.Commands;
using Stl.Multitenancy;

namespace Stl.Fusion.Extensions.Internal;

public partial class SandboxedKeyValueStore : ISandboxedKeyValueStore
{
    public record Options
    {
        public string SessionKeyPrefixFormat { get; set; } = "@session/{0}";
        public TimeSpan? SessionKeyExpirationTime { get; set; } = TimeSpan.FromDays(30);
        public string UserKeyPrefixFormat { get; set; } = "@user/{0}";
        public TimeSpan? UserKeyExpirationTime { get; set; } = null;
        public IMomentClock? Clock { get; set; } = null;
    }

    protected Options Settings { get; }
    protected IKeyValueStore Store { get; }
    protected IAuth Auth { get; }
    protected ITenantResolver TenantResolver { get; }
    protected IMomentClock Clock { get; }

    public SandboxedKeyValueStore(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Store = services.GetRequiredService<IKeyValueStore>();
        Auth = services.GetRequiredService<IAuth>();
        TenantResolver = services.GetRequiredService<ITenantResolver>();
        Clock = settings.Clock ?? services.Clocks().SystemClock;
    }

    public virtual async Task Set(SandboxedSetCommand command, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating()) return;

        var keyChecker = await GetKeyChecker(command.Session, cancellationToken).ConfigureAwait(false);
        var items = command.Items;
        var newItems = new (string Key, string Value, Moment? ExpiresAt)[items.Length];
        for (var i = 0; i < items.Length; i++) {
            var item = items[i];
            var expiresAt = item.ExpiresAt;
            keyChecker.CheckKey(item.Key, ref expiresAt);
            newItems[i] = (item.Key, item.Value, expiresAt);
        }

        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);
        await Store.Set(tenant.Id, newItems, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task Remove(SandboxedRemoveCommand command, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating()) return;

        var keyChecker = await GetKeyChecker(command.Session, cancellationToken).ConfigureAwait(false);
        var keys = command.Keys;
        foreach (var t in keys)
            keyChecker.CheckKey(t);

        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);
        await Store.Remove(tenant, keys, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<string?> Get(Session session, string key, CancellationToken cancellationToken = default)
    {
        var keyChecker = await GetKeyChecker(session, cancellationToken).ConfigureAwait(false);
        keyChecker.CheckKey(key);

        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        return await Store.Get(tenant.Id, key, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<int> Count(Session session, string prefix, CancellationToken cancellationToken = default)
    {
        var keyChecker = await GetKeyChecker(session, cancellationToken).ConfigureAwait(false);
        keyChecker.CheckKeyPrefix(prefix);

        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        return await Store.Count(tenant.Id, prefix, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<string[]> ListKeySuffixes(
        Session session, string prefix, PageRef<string> pageRef,
        SortDirection sortDirection = SortDirection.Ascending, CancellationToken cancellationToken = default)
    {
        var keyChecker = await GetKeyChecker(session, cancellationToken).ConfigureAwait(false);
        keyChecker.CheckKeyPrefix(prefix);

        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        return await Store.ListKeySuffixes(tenant.Id, prefix, pageRef, sortDirection, cancellationToken).ConfigureAwait(false);
    }

    [ComputeMethod]
    protected virtual async Task<KeyChecker> GetKeyChecker(
        Session session, CancellationToken cancellationToken = default)
    {
        if (session == Session.Null)
            throw Errors.KeyViolatesSandboxedKeyValueStoreConstraints();

        var user = await Auth.GetUser(session, cancellationToken).ConfigureAwait(false);
        var keyChecker = new KeyChecker() {
            Clock = Clock,
            Prefix = string.Format(CultureInfo.InvariantCulture, Settings.SessionKeyPrefixFormat, session.Id),
            ExpirationTime = Settings.SessionKeyExpirationTime,
        };
        if (user != null)
            keyChecker = keyChecker with {
                SecondaryPrefix = string.Format(CultureInfo.InvariantCulture, Settings.UserKeyPrefixFormat, user.Id),
                SecondaryExpirationTime = Settings.UserKeyExpirationTime,
            };
        return keyChecker;
    }
}
