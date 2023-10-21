using System.Reflection.Emit;

namespace Stl.Interception;

public abstract partial record ArgumentList
{
    protected static readonly ConcurrentDictionary<(Type, MethodInfo), Func<object, ArgumentList, object?>> InvokerCache = new();

    public static readonly ArgumentList Empty = new ArgumentList0();

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public abstract int Length { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList New()
        => Empty;

    public virtual object?[] ToArray() => Array.Empty<object?>();
    public virtual object?[] ToArray(int skipIndex) => Array.Empty<object?>();

    public virtual Type?[]? GetNonDefaultItemTypes()
        => null;

    public virtual Type? GetType(int index)
        => null;
    public virtual T Get<T>(int index)
        => throw new ArgumentOutOfRangeException(nameof(index));
    public virtual object? GetUntyped(int index)
        => throw new ArgumentOutOfRangeException(nameof(index));
    // Virtual non-generic method for frequent operation
    public virtual CancellationToken GetCancellationToken(int index)
        => throw new ArgumentOutOfRangeException(nameof(index));

    public virtual void Set<T>(int index, T value)
         => throw new ArgumentOutOfRangeException(nameof(index));
    public virtual void SetUntyped(int index, object? value)
         => throw new ArgumentOutOfRangeException(nameof(index));
    // Virtual non-generic method for frequent operation
    public virtual void SetCancellationToken(int index, CancellationToken item)
         => throw new ArgumentOutOfRangeException(nameof(index));

    public virtual void SetFrom(ArgumentList other)
    { }

    public virtual ArgumentList Insert<T>(int index, T item)
        => index == 0
            ? New(item)
            : throw new ArgumentOutOfRangeException(nameof(index));

    // Virtual non-generic method for frequent operation
    public virtual ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index == 0
            ? New(item)
            : throw new ArgumentOutOfRangeException(nameof(index));

    public virtual ArgumentList Remove(int index)
        => throw new ArgumentOutOfRangeException(nameof(index));

    public abstract Func<object, ArgumentList, object?> GetInvoker(MethodInfo method);

    public abstract void Read(ArgumentListReader reader);
    public abstract void Write(ArgumentListWriter writer);

    // Equality

    public abstract bool Equals(ArgumentList? other, int skipIndex);
    public abstract int GetHashCode(int skipIndex);
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList0 : ArgumentList
{
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember]
    public override int Length => 0;

    public override string ToString() => "()";

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            if (method1.GetParameters().Length != 0)
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Pop);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            // Call method
            il.Emit(OpCodes.Callvirt, method1);

            // Box return type
            if (method1.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else if (method1.ReturnType.IsValueType)
                il.Emit(OpCodes.Box, method1.ReturnType);
            il.Emit(OpCodes.Ret);
            return (Func<object, ArgumentList, object?>)m.CreateDelegate(typeof(Func<object, ArgumentList, object?>));
        });

    public override void Read(ArgumentListReader reader)
    { }

    public override void Write(ArgumentListWriter writer)
    { }

    // Equality

    public bool Equals(ArgumentList0? other)
        => !ReferenceEquals(other, null);
    public override bool Equals(ArgumentList? other, int skipIndex)
        => other?.GetType() == typeof(ArgumentList0);

    public override int GetHashCode() => 1;
    public override int GetHashCode(int skipIndex) => 1;
}
