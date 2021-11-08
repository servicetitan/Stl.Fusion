namespace Stl.Fusion.Extensions.Internal;

public partial class SandboxedKeyValueStore
{
    public record KeyChecker
    {
        public IMomentClock Clock { get; init; } = null!;
        public string Prefix { get; init; } = "";
        public string? SecondaryPrefix { get; init; }
        public TimeSpan? ExpirationTime { get; init; }
        public TimeSpan? SecondaryExpirationTime { get; init; }

        public virtual void CheckKeyPrefix(string keyPrefix)
        {
            if (keyPrefix.StartsWith(Prefix, StringComparison.Ordinal))
                return;
            if (SecondaryPrefix != null && keyPrefix.StartsWith(SecondaryPrefix, StringComparison.Ordinal))
                return;
            throw Errors.KeyViolatesSandboxedKeyValueStoreConstraints();
        }

        public virtual void CheckKey(string key)
            => CheckKeyPrefix(key);

        public virtual void CheckKey(string key, ref Moment? expiresAt)
        {
            if (key.StartsWith(Prefix, StringComparison.Ordinal)) {
                if (!ExpirationTime.HasValue)
                    return;
                var maxExpiresAt = Clock.Now + ExpirationTime.GetValueOrDefault();
                expiresAt = expiresAt.HasValue
                    ? Moment.Min(maxExpiresAt, expiresAt.GetValueOrDefault())
                    : maxExpiresAt;
                return;
            }
            if (SecondaryPrefix != null && key.StartsWith(SecondaryPrefix, StringComparison.Ordinal)) {
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
