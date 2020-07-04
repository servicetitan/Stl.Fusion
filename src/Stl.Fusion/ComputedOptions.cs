using System;
using Newtonsoft.Json;

namespace Stl.Fusion
{
    [Serializable]
    public class ComputedOptions
    {
        public static readonly TimeSpan DefaultKeepAliveTime = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan DefaultErrorAutoInvalidateTimeout = TimeSpan.FromSeconds(1);
        public static readonly ComputedOptions Default = new ComputedOptions();

        public TimeSpan KeepAliveTime { get; }
        public TimeSpan ErrorAutoInvalidateTimeout { get; }
        public TimeSpan? AutoInvalidateTimeout { get; }

        public ComputedOptions(
            TimeSpan? keepAliveTime = null, 
            TimeSpan? errorAutoInvalidateTimeout = null,
            TimeSpan? autoInvalidateTimeout = null)
            : this(
                keepAliveTime ?? DefaultKeepAliveTime,
                errorAutoInvalidateTimeout ?? DefaultErrorAutoInvalidateTimeout,
                autoInvalidateTimeout)
        { }

        [JsonConstructor]
        public ComputedOptions(
            TimeSpan keepAliveTime, 
            TimeSpan errorAutoInvalidateTimeout,
            TimeSpan? autoInvalidateTimeout)
        {
            KeepAliveTime = keepAliveTime;
            ErrorAutoInvalidateTimeout = errorAutoInvalidateTimeout;
            AutoInvalidateTimeout = autoInvalidateTimeout;
            if (autoInvalidateTimeout.HasValue) {
                var ait = AutoInvalidateTimeout.GetValueOrDefault();
                if (ErrorAutoInvalidateTimeout > ait)
                    // It just doesn't make sense to keep it higher
                    ErrorAutoInvalidateTimeout = ait;
            }
        }
    }
}
