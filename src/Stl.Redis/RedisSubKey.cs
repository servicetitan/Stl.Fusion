using StackExchange.Redis;

namespace Stl.Redis;

[StructLayout(LayoutKind.Auto)]
[Serializable]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
[method: Newtonsoft.Json.JsonConstructor, JsonConstructor, MemoryPackConstructor]
public readonly partial struct RedisSubKey(string key, RedisChannel.PatternMode patternMode)
{
    [DataMember(Order = 0), MemoryPackOrder(0)]
    public string Key { get; } = key;

    [DataMember(Order = 1), MemoryPackOrder(1)]
    public RedisChannel.PatternMode PatternMode { get; } = patternMode;

    public RedisSubKey(string key) : this(key, RedisChannel.PatternMode.Auto) { }

    public override string ToString()
        => $"({JsonFormatter.Format(Key)}, {PatternMode})";

    public static implicit operator RedisSubKey(string key) => new(key);
    public static implicit operator RedisSubKey((string Key, RedisChannel.PatternMode PatternMode) pair)
        => new(pair.Key, pair.PatternMode);
}
