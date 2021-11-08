using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Extensions.Internal;

public partial class SandboxedKeyValueStore : ISandboxedKeyValueStore
{
    public class Options
    {
        public string SessionKeyPrefixFormat { get; set; } = "@session/{0}";
        public TimeSpan? SessionKeyExpirationTime { get; set; } = TimeSpan.FromDays(30);
        public string UserKeyPrefixFormat { get; set; } = "@user/{0}";
        public TimeSpan? UserKeyExpirationTime { get; set; } = null;
        public IMomentClock? Clock { get; set; } = null;
    }

    protected IKeyValueStore Store { get; }
    protected IAuth Auth { get; }
    protected IMomentClock Clock { get; }
    public string SessionKeyPrefixFormat { get; }
    public TimeSpan? SessionKeyExpirationTime { get; }
    public string UserKeyPrefixFormat { get; }
    public TimeSpan? UserKeyExpirationTime { get; }

    public SandboxedKeyValueStore(Options? options, IServiceProvider services)
    {
        options ??= new Options();
        SessionKeyPrefixFormat = options.SessionKeyPrefixFormat;
        SessionKeyExpirationTime = options.SessionKeyExpirationTime;
        UserKeyPrefixFormat = options.UserKeyPrefixFormat;
        UserKeyExpirationTime = options.UserKeyExpirationTime;
        Store = services.GetRequiredService<IKeyValueStore>();
        Auth = services.GetRequiredService<IAuth>();
        Clock = options.Clock ?? services.SystemClock();
    }

    public virtual async Task Set(SandboxedSetCommand command, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating()) return;
        var keyChecker = await GetKeyChecker(command.Session, cancellationToken).ConfigureAwait(false);
        var expiresAt = command.ExpiresAt;
        keyChecker.CheckKey(command.Key, ref expiresAt);
        await Store.Set(command.Key, command.Value, expiresAt, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task SetMany(SandboxedSetManyCommand command, CancellationToken cancellationToken = default)
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
        await Store.SetMany(newItems, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task Remove(SandboxedRemoveCommand command, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating()) return;
        var keyChecker = await GetKeyChecker(command.Session, cancellationToken).ConfigureAwait(false);
        keyChecker.CheckKey(command.Key);
        await Store.Remove(command.Key, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task RemoveMany(SandboxedRemoveManyCommand command, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating()) return;
        var keyChecker = await GetKeyChecker(command.Session, cancellationToken).ConfigureAwait(false);
        var keys = command.Keys;
        foreach (var t in keys)
            keyChecker.CheckKey(t);
        await Store.RemoveMany(keys, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<string?> Get(Session session, string key, CancellationToken cancellationToken = default)
    {
        var keyChecker = await GetKeyChecker(session, cancellationToken).ConfigureAwait(false);
        keyChecker.CheckKey(key);
        return await Store.Get(key, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<int> Count(Session session, string prefix, CancellationToken cancellationToken = default)
    {
        var keyChecker = await GetKeyChecker(session, cancellationToken).ConfigureAwait(false);
        keyChecker.CheckKeyPrefix(prefix);
        return await Store.Count(prefix, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<string[]> ListKeySuffixes(
        Session session, string prefix, PageRef<string> pageRef,
        SortDirection sortDirection = SortDirection.Ascending, CancellationToken cancellationToken = default)
    {
        var keyChecker = await GetKeyChecker(session, cancellationToken).ConfigureAwait(false);
        keyChecker.CheckKeyPrefix(prefix);
        return await Store.ListKeySuffixes(prefix, pageRef, sortDirection, cancellationToken).ConfigureAwait(false);
    }

    [ComputeMethod]
    protected virtual async Task<KeyChecker> GetKeyChecker(
        Session session, CancellationToken cancellationToken = default)
    {
        if (session == Session.Null)
            throw Errors.KeyViolatesSandboxedKeyValueStoreConstraints();
        var user = await Auth.GetSessionUser(session, cancellationToken).ConfigureAwait(false);
        if (!user.IsAuthenticated)
            return new KeyChecker() {
                Clock = Clock,
                Prefix = string.Format(CultureInfo.InvariantCulture, SessionKeyPrefixFormat, session.Id),
                ExpirationTime = SessionKeyExpirationTime,
            };
        return new KeyChecker() {
            Clock = Clock,
            Prefix = string.Format(CultureInfo.InvariantCulture, SessionKeyPrefixFormat, session.Id),
            ExpirationTime = SessionKeyExpirationTime,
            SecondaryPrefix = string.Format(CultureInfo.InvariantCulture, UserKeyPrefixFormat, user.Id),
            SecondaryExpirationTime = UserKeyExpirationTime,
        };
    }
}
