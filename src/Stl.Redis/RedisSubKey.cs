using StackExchange.Redis;

namespace Stl.Redis;

[Serializable]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public readonly partial struct RedisSubKey
{
    [DataMember(Order = 0), MemoryPackOrder(0)]
    public string Key { get; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public RedisChannel.PatternMode PatternMode { get; }

    public RedisSubKey(string key) : this(key, RedisChannel.PatternMode.Auto) { }
    [Newtonsoft.Json.JsonConstructor, JsonConstructor, MemoryPackConstructor]
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
