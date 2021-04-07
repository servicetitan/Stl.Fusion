using Stl.Time;

namespace Stl.Fusion.Extensions.Internal
{
    public readonly struct KeyValueStoreSandbox
    {
        public string KeyPrefix { get; }
        public Moment? MaxExpiresAt { get; }

        public KeyValueStoreSandbox(string keyPrefix, Moment? maxExpiresAt)
        {
            KeyPrefix = keyPrefix;
            MaxExpiresAt = maxExpiresAt;
        }

        public string Apply(string key)
            => KeyPrefix + key;

        public Moment? Apply(Moment? expiresAt)
        {
            if (!MaxExpiresAt.HasValue)
                return expiresAt;
            var maxExpiresAt = MaxExpiresAt.GetValueOrDefault();

            if (!expiresAt.HasValue)
                return maxExpiresAt;
            return Moment.Min(maxExpiresAt, expiresAt.GetValueOrDefault());
        }
    }
}
