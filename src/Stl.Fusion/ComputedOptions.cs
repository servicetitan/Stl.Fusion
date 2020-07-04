using System;
using Newtonsoft.Json;

namespace Stl.Fusion
{
    [Serializable]
    public class ComputedOptions
    {
        public static readonly TimeSpan DefaultKeepAliveTime = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan DefaultErrorAutoInvalidateTime = TimeSpan.FromSeconds(1);
        public static readonly ComputedOptions Default = new ComputedOptions();

        public TimeSpan KeepAliveTime { get; }
        public TimeSpan ErrorAutoInvalidateTime { get; }
        public TimeSpan? AutoInvalidateTime { get; }

        public ComputedOptions(
            TimeSpan? keepAliveTime = null, 
            TimeSpan? errorAutoInvalidateTime = null,
            TimeSpan? autoInvalidateTime = null)
            : this(
                keepAliveTime ?? DefaultKeepAliveTime,
                errorAutoInvalidateTime ?? DefaultErrorAutoInvalidateTime,
                autoInvalidateTime)
        { }

        [JsonConstructor]
        public ComputedOptions(
            TimeSpan keepAliveTime, 
            TimeSpan errorAutoInvalidateTime,
            TimeSpan? autoInvalidateTime)
        {
            KeepAliveTime = keepAliveTime;
            ErrorAutoInvalidateTime = errorAutoInvalidateTime;
            AutoInvalidateTime = autoInvalidateTime;
            if (autoInvalidateTime.HasValue) {
                var ait = AutoInvalidateTime.GetValueOrDefault();
                if (ErrorAutoInvalidateTime > ait)
                    // It just doesn't make sense to keep it higher
                    ErrorAutoInvalidateTime = ait;
            }
        }
    }
}
