using System;
using Stl.Time;

namespace Stl.Fusion.Extensions.Internal
{
    public partial class SandboxedKeyValueStore
    {
        public record KeyChecker
        {
            public string Prefix { get; init; } = "";
            public string? SecondaryPrefix { get; init; }
            public TimeSpan? ExpirationTime { get; init; }
            public TimeSpan? SecondaryExpirationTime { get; init; }
            public IMomentClock Clock { get; init; } = SystemClock.Instance;

            public virtual void CheckKeyPrefix(string keyPrefix)
            {
                if (keyPrefix.StartsWith(Prefix))
                    return;
                if (SecondaryPrefix != null && keyPrefix.StartsWith(SecondaryPrefix))
                    return;
                throw Errors.KeyViolatesSandboxedKeyValueStoreConstraints();
            }

            public virtual void CheckKey(string key)
                => CheckKeyPrefix(key);

            public virtual void CheckKey(string key, ref Moment? expiresAt)
            {
                if (key.StartsWith(Prefix)) {
                    if (!ExpirationTime.HasValue)
                        return;
                    var maxExpiresAt = Clock.Now + ExpirationTime.GetValueOrDefault();
                    expiresAt = expiresAt.HasValue
                        ? Moment.Min(maxExpiresAt, expiresAt.GetValueOrDefault())
                        : maxExpiresAt;
                    return;
                }
                if (SecondaryPrefix != null && key.StartsWith(SecondaryPrefix)) {
                    if (!SecondaryExpirationTime.HasValue)
                        return;
                    var maxExpiresAt = Clock.Now + SecondaryExpirationTime.GetValueOrDefault();
                    expiresAt = expiresAt.HasValue
                        ? Moment.Min(maxExpiresAt, expiresAt.GetValueOrDefault())
                        : maxExpiresAt;
                    return;
                }
                throw Errors.KeyViolatesSandboxedKeyValueStoreConstraints();
            }
        }
    }
}
