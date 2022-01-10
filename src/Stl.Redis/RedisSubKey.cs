using StackExchange.Redis;

namespace Stl.Redis;

[Serializable]
[DataContract]
public readonly struct RedisSubKey
{
    [DataMember(Order = 0)]
    public string Key { get; }
    [DataMember(Order = 1)]
    public RedisChannel.PatternMode PatternMode { get; }

    public RedisSubKey(string key) : this(key, RedisChannel.PatternMode.Auto) { }
    public RedisSubKey(string key, RedisChannel.PatternMode patternMode)
    {
        Key = key;
        PatternMode = patternMode;
    }

    public override string ToString()
        => $"({JsonFormatter.Format(Key)}, {PatternMode})";

    public static implicit operator RedisSubKey(string key) => new(key);
    public static implicit operator RedisSubKey((string Key, RedisChannel.PatternMode PatternMode) pair)
        => new(pair.Key, pair.PatternMode);
}
