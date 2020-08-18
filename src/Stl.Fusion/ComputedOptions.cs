using System;
using Newtonsoft.Json;

namespace Stl.Fusion
{
    [Serializable]
    public class ComputedOptions
    {
        public static readonly TimeSpan DefaultKeepAliveTime = TimeSpan.Zero;
        public static readonly TimeSpan DefaultErrorAutoInvalidateTime = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan DefaultAutoInvalidateTime = TimeSpan.MaxValue; // No auto invalidation

        public static readonly ComputedOptions Default = new ComputedOptions();
        public static readonly ComputedOptions NoAutoInvalidateOnError =
            new ComputedOptions(errorAutoInvalidateTime: TimeSpan.MaxValue);

        public TimeSpan KeepAliveTime { get; }
        public TimeSpan ErrorAutoInvalidateTime { get; }
        public TimeSpan AutoInvalidateTime { get; }

        public ComputedOptions(
            TimeSpan? keepAliveTime = null,
            TimeSpan? errorAutoInvalidateTime = null,
            TimeSpan? autoInvalidateTime = null)
            : this(
                keepAliveTime ?? DefaultKeepAliveTime,
                errorAutoInvalidateTime ?? DefaultErrorAutoInvalidateTime,
                autoInvalidateTime ?? DefaultAutoInvalidateTime)
        { }

        [JsonConstructor]
        public ComputedOptions(
            TimeSpan keepAliveTime,
            TimeSpan errorAutoInvalidateTime,
            TimeSpan autoInvalidateTime)
        {
            KeepAliveTime = keepAliveTime;
            ErrorAutoInvalidateTime = errorAutoInvalidateTime;
            AutoInvalidateTime = autoInvalidateTime;
            if (ErrorAutoInvalidateTime > autoInvalidateTime)
                // It just doesn't make sense to keep it higher
                ErrorAutoInvalidateTime = autoInvalidateTime;
        }
    }
}
