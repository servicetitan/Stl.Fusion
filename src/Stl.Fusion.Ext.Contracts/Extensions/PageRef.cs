using System.Globalization;

namespace Stl.Fusion.Extensions;

public abstract record PageRef : IHasJsonCompatibleToString
{
    public static PageRef<TKey> New<TKey>(int count)
        => new(count);
    public static PageRef<TKey> New<TKey>(int count, TKey after)
        => new(count, Option.Some(after));
    public static PageRef<TKey> New<TKey>(int count, Option<TKey> after)
        => new(count, after);

    public static PageRef<TKey> Parse<TKey>(string value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
            ? new PageRef<TKey>(count)
            : SystemJsonSerializer.Default.Read<PageRef<TKey>>(value);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record PageRef<TKey>(
    [property: DataMember(Order = 0), MemoryPackOrder(0)] int Count,
    [property: DataMember(Order = 1), MemoryPackOrder(1)] Option<TKey> After = default
    ) : PageRef
{
    public override string ToString()
        => After.IsNone()
            ? Count.ToString(CultureInfo.InvariantCulture)
            : SystemJsonSerializer.Default.Write(this, GetType());

    public static implicit operator PageRef<TKey>(int count)
        => new(count);
    public static implicit operator PageRef<TKey>((int Count, TKey AfterKey) source)
        => new(source.Count, Option.Some(source.AfterKey));
    public static implicit operator PageRef<TKey>((int Count, Option<TKey> AfterKey) source)
        => new(source.Count, source.AfterKey);
}
