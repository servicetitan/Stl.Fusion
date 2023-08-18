// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeConstructorOrDestructorBody
using Cysharp.Text;
using System.Reflection.Emit;

namespace Stl.Interception;

#pragma warning disable MA0012

public abstract partial record ArgumentList
{
    public static ImmutableArray<Type> Types { get; } = ImmutableArray.Create(new [] {
        typeof(ArgumentList0),
        typeof(ArgumentList<>),
        typeof(ArgumentList<, >),
        typeof(ArgumentList<, , >),
        typeof(ArgumentList<, , , >),
        typeof(ArgumentList<, , , , >),
        typeof(ArgumentList<, , , , , >),
        typeof(ArgumentList<, , , , , , >),
        typeof(ArgumentList<, , , , , , , >),
        typeof(ArgumentList<, , , , , , , , >),
        typeof(ArgumentList<, , , , , , , , , >),
    });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0> New<T0>(T0 item0)
        => new (item0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1> New<T0, T1>(T0 item0, T1 item1)
        => new (item0, item1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1, T2> New<T0, T1, T2>(T0 item0, T1 item1, T2 item2)
        => new (item0, item1, item2);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1, T2, T3> New<T0, T1, T2, T3>(T0 item0, T1 item1, T2 item2, T3 item3)
        => new (item0, item1, item2, item3);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1, T2, T3, T4> New<T0, T1, T2, T3, T4>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4)
        => new (item0, item1, item2, item3, item4);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1, T2, T3, T4, T5> New<T0, T1, T2, T3, T4, T5>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        => new (item0, item1, item2, item3, item4, item5);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6> New<T0, T1, T2, T3, T4, T5, T6>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        => new (item0, item1, item2, item3, item4, item5, item6);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7> New<T0, T1, T2, T3, T4, T5, T6, T7>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        => new (item0, item1, item2, item3, item4, item5, item6, item7);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8> New<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8, item9);

    public virtual T Get0<T>() => throw new IndexOutOfRangeException();
    public virtual T Get1<T>() => throw new IndexOutOfRangeException();
    public virtual T Get2<T>() => throw new IndexOutOfRangeException();
    public virtual T Get3<T>() => throw new IndexOutOfRangeException();
    public virtual T Get4<T>() => throw new IndexOutOfRangeException();
    public virtual T Get5<T>() => throw new IndexOutOfRangeException();
    public virtual T Get6<T>() => throw new IndexOutOfRangeException();
    public virtual T Get7<T>() => throw new IndexOutOfRangeException();
    public virtual T Get8<T>() => throw new IndexOutOfRangeException();
    public virtual T Get9<T>() => throw new IndexOutOfRangeException();
}

public abstract record ArgumentList1 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[1];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 1;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0> : ArgumentList1
{
    private T0 _item0;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0)
    {
        _item0 = item0;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex == 0
            ? Array.Empty<object?>()
            : throw new ArgumentOutOfRangeException(nameof(skipIndex));

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0),
            1 => New(Item0, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0),
            1 => New(Item0, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 1)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0> vOther)
            return false;

        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            return hashCode;
        }
    }
}

public abstract record ArgumentList2 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[2];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 2;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1> : ArgumentList2
{
    private T0 _item0;
    private T1 _item1;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1)
    {
        _item0 = item0;
        _item1 = item1;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1 },
            1 => new object?[] { Item0 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0, Item1),
            1 => New(Item0, item, Item1),
            2 => New(Item0, Item1, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0, Item1),
            1 => New(Item0, item, Item1),
            2 => New(Item0, Item1, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1),
            1 => New(Item0),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1> vOther)
            return false;

        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            return hashCode;
        }
    }
}

public abstract record ArgumentList3 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[3];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 3;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1, T2> : ArgumentList3
{
    private T0 _item0;
    private T1 _item1;
    private T2 _item2;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public T2 Item2 { get => _item2; init => _item2 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
        _item2 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1, T2 item2)
    {
        _item0 = item0;
        _item1 = item1;
        _item2 = item2;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(", ");
        sb.Append(Item2);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1, Item2 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1, Item2 },
            1 => new object?[] { Item0, Item2 },
            2 => new object?[] { Item0, Item1 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        if (!typeof(T2).IsValueType) {
            itemType = _item2?.GetType();
            if (itemType != null && itemType != typeof(T2)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[2] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;
    public override T Get2<T>() => Item2 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            2 => Item2 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            2 => Item2,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            2 => Item2 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = item is T2 item2 ? item2 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
        _item2 = other.Get2<T2>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0, Item1, Item2),
            1 => New(Item0, item, Item1, Item2),
            2 => New(Item0, Item1, item, Item2),
            3 => New(Item0, Item1, Item2, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0, Item1, Item2),
            1 => New(Item0, item, Item1, Item2),
            2 => New(Item0, Item1, item, Item2),
            3 => New(Item0, Item1, Item2, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1, Item2),
            1 => New(Item0, Item2),
            2 => New(Item0, Item1),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 3)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[2].ParameterType != typeof(T2))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item2")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
        if (typeof(T2).IsValueType)
            reader.OnStruct(_item2, 2);
        else
            reader.OnObject(typeof(T2), _item2, 2);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
        if (typeof(T2).IsValueType)
            _item2 = writer.OnStruct<T2>(2);
        else
            _item2 = (T2)writer.OnObject(typeof(T2), 2)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1, T2>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
            return false;
        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1, T2> vOther)
            return false;

        if (skipIndex != 2 && !EqualityComparer<T2>.Default.Equals(Item2, vOther.Item2))
            return false;
        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            hashCode = 397*hashCode + EqualityComparer<T2>.Default.GetHashCode(Item2!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            hashCode = 397*hashCode + (skipIndex == 2 ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2!));
            return hashCode;
        }
    }
}

public abstract record ArgumentList4 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[4];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 4;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1, T2, T3> : ArgumentList4
{
    private T0 _item0;
    private T1 _item1;
    private T2 _item2;
    private T3 _item3;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public T2 Item2 { get => _item2; init => _item2 = value; }
    [DataMember(Order = 3), MemoryPackOrder(3)]
    public T3 Item3 { get => _item3; init => _item3 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
        _item2 = default!;
        _item3 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3)
    {
        _item0 = item0;
        _item1 = item1;
        _item2 = item2;
        _item3 = item3;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(", ");
        sb.Append(Item2);
        sb.Append(", ");
        sb.Append(Item3);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1, Item2, Item3 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1, Item2, Item3 },
            1 => new object?[] { Item0, Item2, Item3 },
            2 => new object?[] { Item0, Item1, Item3 },
            3 => new object?[] { Item0, Item1, Item2 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        if (!typeof(T2).IsValueType) {
            itemType = _item2?.GetType();
            if (itemType != null && itemType != typeof(T2)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[2] = itemType;
            }
        }
        if (!typeof(T3).IsValueType) {
            itemType = _item3?.GetType();
            if (itemType != null && itemType != typeof(T3)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[3] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;
    public override T Get2<T>() => Item2 is T value ? value : default!;
    public override T Get3<T>() => Item3 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            2 => Item2 is T value ? value : default!,
            3 => Item3 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            2 => Item2,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            3 => Item3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            2 => Item2 is CancellationToken value ? value : default!,
            3 => Item3 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = item is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = item is T3 item3 ? item3 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
        _item2 = other.Get2<T2>();
        _item3 = other.Get3<T3>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3),
            1 => New(Item0, item, Item1, Item2, Item3),
            2 => New(Item0, Item1, item, Item2, Item3),
            3 => New(Item0, Item1, Item2, item, Item3),
            4 => New(Item0, Item1, Item2, Item3, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3),
            1 => New(Item0, item, Item1, Item2, Item3),
            2 => New(Item0, Item1, item, Item2, Item3),
            3 => New(Item0, Item1, Item2, item, Item3),
            4 => New(Item0, Item1, Item2, Item3, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1, Item2, Item3),
            1 => New(Item0, Item2, Item3),
            2 => New(Item0, Item1, Item3),
            3 => New(Item0, Item1, Item2),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 4)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[2].ParameterType != typeof(T2))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[3].ParameterType != typeof(T3))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item2")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item3")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
        if (typeof(T2).IsValueType)
            reader.OnStruct(_item2, 2);
        else
            reader.OnObject(typeof(T2), _item2, 2);
        if (typeof(T3).IsValueType)
            reader.OnStruct(_item3, 3);
        else
            reader.OnObject(typeof(T3), _item3, 3);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
        if (typeof(T2).IsValueType)
            _item2 = writer.OnStruct<T2>(2);
        else
            _item2 = (T2)writer.OnObject(typeof(T2), 2)!;
        if (typeof(T3).IsValueType)
            _item3 = writer.OnStruct<T3>(3);
        else
            _item3 = (T3)writer.OnObject(typeof(T3), 3)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1, T2, T3>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3))
            return false;
        if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
            return false;
        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1, T2, T3> vOther)
            return false;

        if (skipIndex != 3 && !EqualityComparer<T3>.Default.Equals(Item3, vOther.Item3))
            return false;
        if (skipIndex != 2 && !EqualityComparer<T2>.Default.Equals(Item2, vOther.Item2))
            return false;
        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            hashCode = 397*hashCode + EqualityComparer<T2>.Default.GetHashCode(Item2!);
            hashCode = 397*hashCode + EqualityComparer<T3>.Default.GetHashCode(Item3!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            hashCode = 397*hashCode + (skipIndex == 2 ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2!));
            hashCode = 397*hashCode + (skipIndex == 3 ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3!));
            return hashCode;
        }
    }
}

public abstract record ArgumentList5 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[5];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 5;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1, T2, T3, T4> : ArgumentList5
{
    private T0 _item0;
    private T1 _item1;
    private T2 _item2;
    private T3 _item3;
    private T4 _item4;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public T2 Item2 { get => _item2; init => _item2 = value; }
    [DataMember(Order = 3), MemoryPackOrder(3)]
    public T3 Item3 { get => _item3; init => _item3 = value; }
    [DataMember(Order = 4), MemoryPackOrder(4)]
    public T4 Item4 { get => _item4; init => _item4 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
        _item2 = default!;
        _item3 = default!;
        _item4 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4)
    {
        _item0 = item0;
        _item1 = item1;
        _item2 = item2;
        _item3 = item3;
        _item4 = item4;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(", ");
        sb.Append(Item2);
        sb.Append(", ");
        sb.Append(Item3);
        sb.Append(", ");
        sb.Append(Item4);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1, Item2, Item3, Item4 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1, Item2, Item3, Item4 },
            1 => new object?[] { Item0, Item2, Item3, Item4 },
            2 => new object?[] { Item0, Item1, Item3, Item4 },
            3 => new object?[] { Item0, Item1, Item2, Item4 },
            4 => new object?[] { Item0, Item1, Item2, Item3 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        if (!typeof(T2).IsValueType) {
            itemType = _item2?.GetType();
            if (itemType != null && itemType != typeof(T2)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[2] = itemType;
            }
        }
        if (!typeof(T3).IsValueType) {
            itemType = _item3?.GetType();
            if (itemType != null && itemType != typeof(T3)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[3] = itemType;
            }
        }
        if (!typeof(T4).IsValueType) {
            itemType = _item4?.GetType();
            if (itemType != null && itemType != typeof(T4)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[4] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            4 => typeof(T4),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;
    public override T Get2<T>() => Item2 is T value ? value : default!;
    public override T Get3<T>() => Item3 is T value ? value : default!;
    public override T Get4<T>() => Item4 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            2 => Item2 is T value ? value : default!,
            3 => Item3 is T value ? value : default!,
            4 => Item4 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            2 => Item2,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            3 => Item3,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            4 => Item4,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            2 => Item2 is CancellationToken value ? value : default!,
            3 => Item3 is CancellationToken value ? value : default!,
            4 => Item4 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = item is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = item is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = item is T4 item4 ? item4 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
        _item2 = other.Get2<T2>();
        _item3 = other.Get3<T3>();
        _item4 = other.Get4<T4>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4),
            1 => New(Item0, item, Item1, Item2, Item3, Item4),
            2 => New(Item0, Item1, item, Item2, Item3, Item4),
            3 => New(Item0, Item1, Item2, item, Item3, Item4),
            4 => New(Item0, Item1, Item2, Item3, item, Item4),
            5 => New(Item0, Item1, Item2, Item3, Item4, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4),
            1 => New(Item0, item, Item1, Item2, Item3, Item4),
            2 => New(Item0, Item1, item, Item2, Item3, Item4),
            3 => New(Item0, Item1, Item2, item, Item3, Item4),
            4 => New(Item0, Item1, Item2, Item3, item, Item4),
            5 => New(Item0, Item1, Item2, Item3, Item4, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1, Item2, Item3, Item4),
            1 => New(Item0, Item2, Item3, Item4),
            2 => New(Item0, Item1, Item3, Item4),
            3 => New(Item0, Item1, Item2, Item4),
            4 => New(Item0, Item1, Item2, Item3),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 5)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[2].ParameterType != typeof(T2))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[3].ParameterType != typeof(T3))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[4].ParameterType != typeof(T4))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item2")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item3")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item4")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
        if (typeof(T2).IsValueType)
            reader.OnStruct(_item2, 2);
        else
            reader.OnObject(typeof(T2), _item2, 2);
        if (typeof(T3).IsValueType)
            reader.OnStruct(_item3, 3);
        else
            reader.OnObject(typeof(T3), _item3, 3);
        if (typeof(T4).IsValueType)
            reader.OnStruct(_item4, 4);
        else
            reader.OnObject(typeof(T4), _item4, 4);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
        if (typeof(T2).IsValueType)
            _item2 = writer.OnStruct<T2>(2);
        else
            _item2 = (T2)writer.OnObject(typeof(T2), 2)!;
        if (typeof(T3).IsValueType)
            _item3 = writer.OnStruct<T3>(3);
        else
            _item3 = (T3)writer.OnObject(typeof(T3), 3)!;
        if (typeof(T4).IsValueType)
            _item4 = writer.OnStruct<T4>(4);
        else
            _item4 = (T4)writer.OnObject(typeof(T4), 4)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4))
            return false;
        if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3))
            return false;
        if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
            return false;
        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1, T2, T3, T4> vOther)
            return false;

        if (skipIndex != 4 && !EqualityComparer<T4>.Default.Equals(Item4, vOther.Item4))
            return false;
        if (skipIndex != 3 && !EqualityComparer<T3>.Default.Equals(Item3, vOther.Item3))
            return false;
        if (skipIndex != 2 && !EqualityComparer<T2>.Default.Equals(Item2, vOther.Item2))
            return false;
        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            hashCode = 397*hashCode + EqualityComparer<T2>.Default.GetHashCode(Item2!);
            hashCode = 397*hashCode + EqualityComparer<T3>.Default.GetHashCode(Item3!);
            hashCode = 397*hashCode + EqualityComparer<T4>.Default.GetHashCode(Item4!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            hashCode = 397*hashCode + (skipIndex == 2 ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2!));
            hashCode = 397*hashCode + (skipIndex == 3 ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3!));
            hashCode = 397*hashCode + (skipIndex == 4 ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4!));
            return hashCode;
        }
    }
}

public abstract record ArgumentList6 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[6];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 6;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1, T2, T3, T4, T5> : ArgumentList6
{
    private T0 _item0;
    private T1 _item1;
    private T2 _item2;
    private T3 _item3;
    private T4 _item4;
    private T5 _item5;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public T2 Item2 { get => _item2; init => _item2 = value; }
    [DataMember(Order = 3), MemoryPackOrder(3)]
    public T3 Item3 { get => _item3; init => _item3 = value; }
    [DataMember(Order = 4), MemoryPackOrder(4)]
    public T4 Item4 { get => _item4; init => _item4 = value; }
    [DataMember(Order = 5), MemoryPackOrder(5)]
    public T5 Item5 { get => _item5; init => _item5 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
        _item2 = default!;
        _item3 = default!;
        _item4 = default!;
        _item5 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
    {
        _item0 = item0;
        _item1 = item1;
        _item2 = item2;
        _item3 = item3;
        _item4 = item4;
        _item5 = item5;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(", ");
        sb.Append(Item2);
        sb.Append(", ");
        sb.Append(Item3);
        sb.Append(", ");
        sb.Append(Item4);
        sb.Append(", ");
        sb.Append(Item5);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1, Item2, Item3, Item4, Item5 },
            1 => new object?[] { Item0, Item2, Item3, Item4, Item5 },
            2 => new object?[] { Item0, Item1, Item3, Item4, Item5 },
            3 => new object?[] { Item0, Item1, Item2, Item4, Item5 },
            4 => new object?[] { Item0, Item1, Item2, Item3, Item5 },
            5 => new object?[] { Item0, Item1, Item2, Item3, Item4 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        if (!typeof(T2).IsValueType) {
            itemType = _item2?.GetType();
            if (itemType != null && itemType != typeof(T2)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[2] = itemType;
            }
        }
        if (!typeof(T3).IsValueType) {
            itemType = _item3?.GetType();
            if (itemType != null && itemType != typeof(T3)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[3] = itemType;
            }
        }
        if (!typeof(T4).IsValueType) {
            itemType = _item4?.GetType();
            if (itemType != null && itemType != typeof(T4)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[4] = itemType;
            }
        }
        if (!typeof(T5).IsValueType) {
            itemType = _item5?.GetType();
            if (itemType != null && itemType != typeof(T5)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[5] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            4 => typeof(T4),
            5 => typeof(T5),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;
    public override T Get2<T>() => Item2 is T value ? value : default!;
    public override T Get3<T>() => Item3 is T value ? value : default!;
    public override T Get4<T>() => Item4 is T value ? value : default!;
    public override T Get5<T>() => Item5 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            2 => Item2 is T value ? value : default!,
            3 => Item3 is T value ? value : default!,
            4 => Item4 is T value ? value : default!,
            5 => Item5 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            2 => Item2,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            3 => Item3,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            4 => Item4,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            5 => Item5,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            2 => Item2 is CancellationToken value ? value : default!,
            3 => Item3 is CancellationToken value ? value : default!,
            4 => Item4 is CancellationToken value ? value : default!,
            5 => Item5 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = item is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = item is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = item is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = item is T5 item5 ? item5 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
        _item2 = other.Get2<T2>();
        _item3 = other.Get3<T3>();
        _item4 = other.Get4<T4>();
        _item5 = other.Get5<T5>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4, Item5),
            1 => New(Item0, item, Item1, Item2, Item3, Item4, Item5),
            2 => New(Item0, Item1, item, Item2, Item3, Item4, Item5),
            3 => New(Item0, Item1, Item2, item, Item3, Item4, Item5),
            4 => New(Item0, Item1, Item2, Item3, item, Item4, Item5),
            5 => New(Item0, Item1, Item2, Item3, Item4, item, Item5),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4, Item5),
            1 => New(Item0, item, Item1, Item2, Item3, Item4, Item5),
            2 => New(Item0, Item1, item, Item2, Item3, Item4, Item5),
            3 => New(Item0, Item1, Item2, item, Item3, Item4, Item5),
            4 => New(Item0, Item1, Item2, Item3, item, Item4, Item5),
            5 => New(Item0, Item1, Item2, Item3, Item4, item, Item5),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1, Item2, Item3, Item4, Item5),
            1 => New(Item0, Item2, Item3, Item4, Item5),
            2 => New(Item0, Item1, Item3, Item4, Item5),
            3 => New(Item0, Item1, Item2, Item4, Item5),
            4 => New(Item0, Item1, Item2, Item3, Item5),
            5 => New(Item0, Item1, Item2, Item3, Item4),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 6)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[2].ParameterType != typeof(T2))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[3].ParameterType != typeof(T3))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[4].ParameterType != typeof(T4))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[5].ParameterType != typeof(T5))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item2")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item3")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item4")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item5")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
        if (typeof(T2).IsValueType)
            reader.OnStruct(_item2, 2);
        else
            reader.OnObject(typeof(T2), _item2, 2);
        if (typeof(T3).IsValueType)
            reader.OnStruct(_item3, 3);
        else
            reader.OnObject(typeof(T3), _item3, 3);
        if (typeof(T4).IsValueType)
            reader.OnStruct(_item4, 4);
        else
            reader.OnObject(typeof(T4), _item4, 4);
        if (typeof(T5).IsValueType)
            reader.OnStruct(_item5, 5);
        else
            reader.OnObject(typeof(T5), _item5, 5);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
        if (typeof(T2).IsValueType)
            _item2 = writer.OnStruct<T2>(2);
        else
            _item2 = (T2)writer.OnObject(typeof(T2), 2)!;
        if (typeof(T3).IsValueType)
            _item3 = writer.OnStruct<T3>(3);
        else
            _item3 = (T3)writer.OnObject(typeof(T3), 3)!;
        if (typeof(T4).IsValueType)
            _item4 = writer.OnStruct<T4>(4);
        else
            _item4 = (T4)writer.OnObject(typeof(T4), 4)!;
        if (typeof(T5).IsValueType)
            _item5 = writer.OnStruct<T5>(5);
        else
            _item5 = (T5)writer.OnObject(typeof(T5), 5)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5))
            return false;
        if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4))
            return false;
        if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3))
            return false;
        if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
            return false;
        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1, T2, T3, T4, T5> vOther)
            return false;

        if (skipIndex != 5 && !EqualityComparer<T5>.Default.Equals(Item5, vOther.Item5))
            return false;
        if (skipIndex != 4 && !EqualityComparer<T4>.Default.Equals(Item4, vOther.Item4))
            return false;
        if (skipIndex != 3 && !EqualityComparer<T3>.Default.Equals(Item3, vOther.Item3))
            return false;
        if (skipIndex != 2 && !EqualityComparer<T2>.Default.Equals(Item2, vOther.Item2))
            return false;
        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            hashCode = 397*hashCode + EqualityComparer<T2>.Default.GetHashCode(Item2!);
            hashCode = 397*hashCode + EqualityComparer<T3>.Default.GetHashCode(Item3!);
            hashCode = 397*hashCode + EqualityComparer<T4>.Default.GetHashCode(Item4!);
            hashCode = 397*hashCode + EqualityComparer<T5>.Default.GetHashCode(Item5!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            hashCode = 397*hashCode + (skipIndex == 2 ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2!));
            hashCode = 397*hashCode + (skipIndex == 3 ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3!));
            hashCode = 397*hashCode + (skipIndex == 4 ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4!));
            hashCode = 397*hashCode + (skipIndex == 5 ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5!));
            return hashCode;
        }
    }
}

public abstract record ArgumentList7 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[7];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 7;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1, T2, T3, T4, T5, T6> : ArgumentList7
{
    private T0 _item0;
    private T1 _item1;
    private T2 _item2;
    private T3 _item3;
    private T4 _item4;
    private T5 _item5;
    private T6 _item6;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public T2 Item2 { get => _item2; init => _item2 = value; }
    [DataMember(Order = 3), MemoryPackOrder(3)]
    public T3 Item3 { get => _item3; init => _item3 = value; }
    [DataMember(Order = 4), MemoryPackOrder(4)]
    public T4 Item4 { get => _item4; init => _item4 = value; }
    [DataMember(Order = 5), MemoryPackOrder(5)]
    public T5 Item5 { get => _item5; init => _item5 = value; }
    [DataMember(Order = 6), MemoryPackOrder(6)]
    public T6 Item6 { get => _item6; init => _item6 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
        _item2 = default!;
        _item3 = default!;
        _item4 = default!;
        _item5 = default!;
        _item6 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
    {
        _item0 = item0;
        _item1 = item1;
        _item2 = item2;
        _item3 = item3;
        _item4 = item4;
        _item5 = item5;
        _item6 = item6;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(", ");
        sb.Append(Item2);
        sb.Append(", ");
        sb.Append(Item3);
        sb.Append(", ");
        sb.Append(Item4);
        sb.Append(", ");
        sb.Append(Item5);
        sb.Append(", ");
        sb.Append(Item6);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1, Item2, Item3, Item4, Item5, Item6 },
            1 => new object?[] { Item0, Item2, Item3, Item4, Item5, Item6 },
            2 => new object?[] { Item0, Item1, Item3, Item4, Item5, Item6 },
            3 => new object?[] { Item0, Item1, Item2, Item4, Item5, Item6 },
            4 => new object?[] { Item0, Item1, Item2, Item3, Item5, Item6 },
            5 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item6 },
            6 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        if (!typeof(T2).IsValueType) {
            itemType = _item2?.GetType();
            if (itemType != null && itemType != typeof(T2)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[2] = itemType;
            }
        }
        if (!typeof(T3).IsValueType) {
            itemType = _item3?.GetType();
            if (itemType != null && itemType != typeof(T3)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[3] = itemType;
            }
        }
        if (!typeof(T4).IsValueType) {
            itemType = _item4?.GetType();
            if (itemType != null && itemType != typeof(T4)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[4] = itemType;
            }
        }
        if (!typeof(T5).IsValueType) {
            itemType = _item5?.GetType();
            if (itemType != null && itemType != typeof(T5)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[5] = itemType;
            }
        }
        if (!typeof(T6).IsValueType) {
            itemType = _item6?.GetType();
            if (itemType != null && itemType != typeof(T6)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[6] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            4 => typeof(T4),
            5 => typeof(T5),
            6 => typeof(T6),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;
    public override T Get2<T>() => Item2 is T value ? value : default!;
    public override T Get3<T>() => Item3 is T value ? value : default!;
    public override T Get4<T>() => Item4 is T value ? value : default!;
    public override T Get5<T>() => Item5 is T value ? value : default!;
    public override T Get6<T>() => Item6 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            2 => Item2 is T value ? value : default!,
            3 => Item3 is T value ? value : default!,
            4 => Item4 is T value ? value : default!,
            5 => Item5 is T value ? value : default!,
            6 => Item6 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            2 => Item2,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            3 => Item3,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            4 => Item4,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            5 => Item5,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            6 => Item6,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            2 => Item2 is CancellationToken value ? value : default!,
            3 => Item3 is CancellationToken value ? value : default!,
            4 => Item4 is CancellationToken value ? value : default!,
            5 => Item5 is CancellationToken value ? value : default!,
            6 => Item6 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = value is T6 item6 ? item6 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = value is T6 item6 ? item6 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = item is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = item is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = item is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = item is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = item is T6 item6 ? item6 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
        _item2 = other.Get2<T2>();
        _item3 = other.Get3<T3>();
        _item4 = other.Get4<T4>();
        _item5 = other.Get5<T5>();
        _item6 = other.Get6<T6>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4, Item5, Item6),
            1 => New(Item0, item, Item1, Item2, Item3, Item4, Item5, Item6),
            2 => New(Item0, Item1, item, Item2, Item3, Item4, Item5, Item6),
            3 => New(Item0, Item1, Item2, item, Item3, Item4, Item5, Item6),
            4 => New(Item0, Item1, Item2, Item3, item, Item4, Item5, Item6),
            5 => New(Item0, Item1, Item2, Item3, Item4, item, Item5, Item6),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, item, Item6),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4, Item5, Item6),
            1 => New(Item0, item, Item1, Item2, Item3, Item4, Item5, Item6),
            2 => New(Item0, Item1, item, Item2, Item3, Item4, Item5, Item6),
            3 => New(Item0, Item1, Item2, item, Item3, Item4, Item5, Item6),
            4 => New(Item0, Item1, Item2, Item3, item, Item4, Item5, Item6),
            5 => New(Item0, Item1, Item2, Item3, Item4, item, Item5, Item6),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, item, Item6),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1, Item2, Item3, Item4, Item5, Item6),
            1 => New(Item0, Item2, Item3, Item4, Item5, Item6),
            2 => New(Item0, Item1, Item3, Item4, Item5, Item6),
            3 => New(Item0, Item1, Item2, Item4, Item5, Item6),
            4 => New(Item0, Item1, Item2, Item3, Item5, Item6),
            5 => New(Item0, Item1, Item2, Item3, Item4, Item6),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 7)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[2].ParameterType != typeof(T2))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[3].ParameterType != typeof(T3))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[4].ParameterType != typeof(T4))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[5].ParameterType != typeof(T5))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[6].ParameterType != typeof(T6))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item2")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item3")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item4")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item5")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item6")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
        if (typeof(T2).IsValueType)
            reader.OnStruct(_item2, 2);
        else
            reader.OnObject(typeof(T2), _item2, 2);
        if (typeof(T3).IsValueType)
            reader.OnStruct(_item3, 3);
        else
            reader.OnObject(typeof(T3), _item3, 3);
        if (typeof(T4).IsValueType)
            reader.OnStruct(_item4, 4);
        else
            reader.OnObject(typeof(T4), _item4, 4);
        if (typeof(T5).IsValueType)
            reader.OnStruct(_item5, 5);
        else
            reader.OnObject(typeof(T5), _item5, 5);
        if (typeof(T6).IsValueType)
            reader.OnStruct(_item6, 6);
        else
            reader.OnObject(typeof(T6), _item6, 6);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
        if (typeof(T2).IsValueType)
            _item2 = writer.OnStruct<T2>(2);
        else
            _item2 = (T2)writer.OnObject(typeof(T2), 2)!;
        if (typeof(T3).IsValueType)
            _item3 = writer.OnStruct<T3>(3);
        else
            _item3 = (T3)writer.OnObject(typeof(T3), 3)!;
        if (typeof(T4).IsValueType)
            _item4 = writer.OnStruct<T4>(4);
        else
            _item4 = (T4)writer.OnObject(typeof(T4), 4)!;
        if (typeof(T5).IsValueType)
            _item5 = writer.OnStruct<T5>(5);
        else
            _item5 = (T5)writer.OnObject(typeof(T5), 5)!;
        if (typeof(T6).IsValueType)
            _item6 = writer.OnStruct<T6>(6);
        else
            _item6 = (T6)writer.OnObject(typeof(T6), 6)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6))
            return false;
        if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5))
            return false;
        if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4))
            return false;
        if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3))
            return false;
        if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
            return false;
        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1, T2, T3, T4, T5, T6> vOther)
            return false;

        if (skipIndex != 6 && !EqualityComparer<T6>.Default.Equals(Item6, vOther.Item6))
            return false;
        if (skipIndex != 5 && !EqualityComparer<T5>.Default.Equals(Item5, vOther.Item5))
            return false;
        if (skipIndex != 4 && !EqualityComparer<T4>.Default.Equals(Item4, vOther.Item4))
            return false;
        if (skipIndex != 3 && !EqualityComparer<T3>.Default.Equals(Item3, vOther.Item3))
            return false;
        if (skipIndex != 2 && !EqualityComparer<T2>.Default.Equals(Item2, vOther.Item2))
            return false;
        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            hashCode = 397*hashCode + EqualityComparer<T2>.Default.GetHashCode(Item2!);
            hashCode = 397*hashCode + EqualityComparer<T3>.Default.GetHashCode(Item3!);
            hashCode = 397*hashCode + EqualityComparer<T4>.Default.GetHashCode(Item4!);
            hashCode = 397*hashCode + EqualityComparer<T5>.Default.GetHashCode(Item5!);
            hashCode = 397*hashCode + EqualityComparer<T6>.Default.GetHashCode(Item6!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            hashCode = 397*hashCode + (skipIndex == 2 ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2!));
            hashCode = 397*hashCode + (skipIndex == 3 ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3!));
            hashCode = 397*hashCode + (skipIndex == 4 ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4!));
            hashCode = 397*hashCode + (skipIndex == 5 ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5!));
            hashCode = 397*hashCode + (skipIndex == 6 ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6!));
            return hashCode;
        }
    }
}

public abstract record ArgumentList8 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[8];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 8;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7> : ArgumentList8
{
    private T0 _item0;
    private T1 _item1;
    private T2 _item2;
    private T3 _item3;
    private T4 _item4;
    private T5 _item5;
    private T6 _item6;
    private T7 _item7;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public T2 Item2 { get => _item2; init => _item2 = value; }
    [DataMember(Order = 3), MemoryPackOrder(3)]
    public T3 Item3 { get => _item3; init => _item3 = value; }
    [DataMember(Order = 4), MemoryPackOrder(4)]
    public T4 Item4 { get => _item4; init => _item4 = value; }
    [DataMember(Order = 5), MemoryPackOrder(5)]
    public T5 Item5 { get => _item5; init => _item5 = value; }
    [DataMember(Order = 6), MemoryPackOrder(6)]
    public T6 Item6 { get => _item6; init => _item6 = value; }
    [DataMember(Order = 7), MemoryPackOrder(7)]
    public T7 Item7 { get => _item7; init => _item7 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
        _item2 = default!;
        _item3 = default!;
        _item4 = default!;
        _item5 = default!;
        _item6 = default!;
        _item7 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
    {
        _item0 = item0;
        _item1 = item1;
        _item2 = item2;
        _item3 = item3;
        _item4 = item4;
        _item5 = item5;
        _item6 = item6;
        _item7 = item7;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(", ");
        sb.Append(Item2);
        sb.Append(", ");
        sb.Append(Item3);
        sb.Append(", ");
        sb.Append(Item4);
        sb.Append(", ");
        sb.Append(Item5);
        sb.Append(", ");
        sb.Append(Item6);
        sb.Append(", ");
        sb.Append(Item7);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1, Item2, Item3, Item4, Item5, Item6, Item7 },
            1 => new object?[] { Item0, Item2, Item3, Item4, Item5, Item6, Item7 },
            2 => new object?[] { Item0, Item1, Item3, Item4, Item5, Item6, Item7 },
            3 => new object?[] { Item0, Item1, Item2, Item4, Item5, Item6, Item7 },
            4 => new object?[] { Item0, Item1, Item2, Item3, Item5, Item6, Item7 },
            5 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item6, Item7 },
            6 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item7 },
            7 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        if (!typeof(T2).IsValueType) {
            itemType = _item2?.GetType();
            if (itemType != null && itemType != typeof(T2)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[2] = itemType;
            }
        }
        if (!typeof(T3).IsValueType) {
            itemType = _item3?.GetType();
            if (itemType != null && itemType != typeof(T3)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[3] = itemType;
            }
        }
        if (!typeof(T4).IsValueType) {
            itemType = _item4?.GetType();
            if (itemType != null && itemType != typeof(T4)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[4] = itemType;
            }
        }
        if (!typeof(T5).IsValueType) {
            itemType = _item5?.GetType();
            if (itemType != null && itemType != typeof(T5)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[5] = itemType;
            }
        }
        if (!typeof(T6).IsValueType) {
            itemType = _item6?.GetType();
            if (itemType != null && itemType != typeof(T6)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[6] = itemType;
            }
        }
        if (!typeof(T7).IsValueType) {
            itemType = _item7?.GetType();
            if (itemType != null && itemType != typeof(T7)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[7] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            4 => typeof(T4),
            5 => typeof(T5),
            6 => typeof(T6),
            7 => typeof(T7),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;
    public override T Get2<T>() => Item2 is T value ? value : default!;
    public override T Get3<T>() => Item3 is T value ? value : default!;
    public override T Get4<T>() => Item4 is T value ? value : default!;
    public override T Get5<T>() => Item5 is T value ? value : default!;
    public override T Get6<T>() => Item6 is T value ? value : default!;
    public override T Get7<T>() => Item7 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            2 => Item2 is T value ? value : default!,
            3 => Item3 is T value ? value : default!,
            4 => Item4 is T value ? value : default!,
            5 => Item5 is T value ? value : default!,
            6 => Item6 is T value ? value : default!,
            7 => Item7 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            2 => Item2,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            3 => Item3,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            4 => Item4,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            5 => Item5,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            6 => Item6,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            7 => Item7,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            2 => Item2 is CancellationToken value ? value : default!,
            3 => Item3 is CancellationToken value ? value : default!,
            4 => Item4 is CancellationToken value ? value : default!,
            5 => Item5 is CancellationToken value ? value : default!,
            6 => Item6 is CancellationToken value ? value : default!,
            7 => Item7 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = value is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = value is T7 item7 ? item7 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = value is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = value is T7 item7 ? item7 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = item is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = item is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = item is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = item is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = item is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = item is T7 item7 ? item7 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
        _item2 = other.Get2<T2>();
        _item3 = other.Get3<T3>();
        _item4 = other.Get4<T4>();
        _item5 = other.Get5<T5>();
        _item6 = other.Get6<T6>();
        _item7 = other.Get7<T7>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7),
            1 => New(Item0, item, Item1, Item2, Item3, Item4, Item5, Item6, Item7),
            2 => New(Item0, Item1, item, Item2, Item3, Item4, Item5, Item6, Item7),
            3 => New(Item0, Item1, Item2, item, Item3, Item4, Item5, Item6, Item7),
            4 => New(Item0, Item1, Item2, Item3, item, Item4, Item5, Item6, Item7),
            5 => New(Item0, Item1, Item2, Item3, Item4, item, Item5, Item6, Item7),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, item, Item6, Item7),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, item, Item7),
            8 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7),
            1 => New(Item0, item, Item1, Item2, Item3, Item4, Item5, Item6, Item7),
            2 => New(Item0, Item1, item, Item2, Item3, Item4, Item5, Item6, Item7),
            3 => New(Item0, Item1, Item2, item, Item3, Item4, Item5, Item6, Item7),
            4 => New(Item0, Item1, Item2, Item3, item, Item4, Item5, Item6, Item7),
            5 => New(Item0, Item1, Item2, Item3, Item4, item, Item5, Item6, Item7),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, item, Item6, Item7),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, item, Item7),
            8 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1, Item2, Item3, Item4, Item5, Item6, Item7),
            1 => New(Item0, Item2, Item3, Item4, Item5, Item6, Item7),
            2 => New(Item0, Item1, Item3, Item4, Item5, Item6, Item7),
            3 => New(Item0, Item1, Item2, Item4, Item5, Item6, Item7),
            4 => New(Item0, Item1, Item2, Item3, Item5, Item6, Item7),
            5 => New(Item0, Item1, Item2, Item3, Item4, Item6, Item7),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item7),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 8)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[2].ParameterType != typeof(T2))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[3].ParameterType != typeof(T3))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[4].ParameterType != typeof(T4))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[5].ParameterType != typeof(T5))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[6].ParameterType != typeof(T6))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[7].ParameterType != typeof(T7))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item2")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item3")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item4")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item5")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item6")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item7")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
        if (typeof(T2).IsValueType)
            reader.OnStruct(_item2, 2);
        else
            reader.OnObject(typeof(T2), _item2, 2);
        if (typeof(T3).IsValueType)
            reader.OnStruct(_item3, 3);
        else
            reader.OnObject(typeof(T3), _item3, 3);
        if (typeof(T4).IsValueType)
            reader.OnStruct(_item4, 4);
        else
            reader.OnObject(typeof(T4), _item4, 4);
        if (typeof(T5).IsValueType)
            reader.OnStruct(_item5, 5);
        else
            reader.OnObject(typeof(T5), _item5, 5);
        if (typeof(T6).IsValueType)
            reader.OnStruct(_item6, 6);
        else
            reader.OnObject(typeof(T6), _item6, 6);
        if (typeof(T7).IsValueType)
            reader.OnStruct(_item7, 7);
        else
            reader.OnObject(typeof(T7), _item7, 7);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
        if (typeof(T2).IsValueType)
            _item2 = writer.OnStruct<T2>(2);
        else
            _item2 = (T2)writer.OnObject(typeof(T2), 2)!;
        if (typeof(T3).IsValueType)
            _item3 = writer.OnStruct<T3>(3);
        else
            _item3 = (T3)writer.OnObject(typeof(T3), 3)!;
        if (typeof(T4).IsValueType)
            _item4 = writer.OnStruct<T4>(4);
        else
            _item4 = (T4)writer.OnObject(typeof(T4), 4)!;
        if (typeof(T5).IsValueType)
            _item5 = writer.OnStruct<T5>(5);
        else
            _item5 = (T5)writer.OnObject(typeof(T5), 5)!;
        if (typeof(T6).IsValueType)
            _item6 = writer.OnStruct<T6>(6);
        else
            _item6 = (T6)writer.OnObject(typeof(T6), 6)!;
        if (typeof(T7).IsValueType)
            _item7 = writer.OnStruct<T7>(7);
        else
            _item7 = (T7)writer.OnObject(typeof(T7), 7)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7))
            return false;
        if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6))
            return false;
        if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5))
            return false;
        if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4))
            return false;
        if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3))
            return false;
        if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
            return false;
        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7> vOther)
            return false;

        if (skipIndex != 7 && !EqualityComparer<T7>.Default.Equals(Item7, vOther.Item7))
            return false;
        if (skipIndex != 6 && !EqualityComparer<T6>.Default.Equals(Item6, vOther.Item6))
            return false;
        if (skipIndex != 5 && !EqualityComparer<T5>.Default.Equals(Item5, vOther.Item5))
            return false;
        if (skipIndex != 4 && !EqualityComparer<T4>.Default.Equals(Item4, vOther.Item4))
            return false;
        if (skipIndex != 3 && !EqualityComparer<T3>.Default.Equals(Item3, vOther.Item3))
            return false;
        if (skipIndex != 2 && !EqualityComparer<T2>.Default.Equals(Item2, vOther.Item2))
            return false;
        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            hashCode = 397*hashCode + EqualityComparer<T2>.Default.GetHashCode(Item2!);
            hashCode = 397*hashCode + EqualityComparer<T3>.Default.GetHashCode(Item3!);
            hashCode = 397*hashCode + EqualityComparer<T4>.Default.GetHashCode(Item4!);
            hashCode = 397*hashCode + EqualityComparer<T5>.Default.GetHashCode(Item5!);
            hashCode = 397*hashCode + EqualityComparer<T6>.Default.GetHashCode(Item6!);
            hashCode = 397*hashCode + EqualityComparer<T7>.Default.GetHashCode(Item7!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            hashCode = 397*hashCode + (skipIndex == 2 ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2!));
            hashCode = 397*hashCode + (skipIndex == 3 ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3!));
            hashCode = 397*hashCode + (skipIndex == 4 ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4!));
            hashCode = 397*hashCode + (skipIndex == 5 ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5!));
            hashCode = 397*hashCode + (skipIndex == 6 ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6!));
            hashCode = 397*hashCode + (skipIndex == 7 ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7!));
            return hashCode;
        }
    }
}

public abstract record ArgumentList9 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[9];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 9;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8> : ArgumentList9
{
    private T0 _item0;
    private T1 _item1;
    private T2 _item2;
    private T3 _item3;
    private T4 _item4;
    private T5 _item5;
    private T6 _item6;
    private T7 _item7;
    private T8 _item8;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public T2 Item2 { get => _item2; init => _item2 = value; }
    [DataMember(Order = 3), MemoryPackOrder(3)]
    public T3 Item3 { get => _item3; init => _item3 = value; }
    [DataMember(Order = 4), MemoryPackOrder(4)]
    public T4 Item4 { get => _item4; init => _item4 = value; }
    [DataMember(Order = 5), MemoryPackOrder(5)]
    public T5 Item5 { get => _item5; init => _item5 = value; }
    [DataMember(Order = 6), MemoryPackOrder(6)]
    public T6 Item6 { get => _item6; init => _item6 = value; }
    [DataMember(Order = 7), MemoryPackOrder(7)]
    public T7 Item7 { get => _item7; init => _item7 = value; }
    [DataMember(Order = 8), MemoryPackOrder(8)]
    public T8 Item8 { get => _item8; init => _item8 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
        _item2 = default!;
        _item3 = default!;
        _item4 = default!;
        _item5 = default!;
        _item6 = default!;
        _item7 = default!;
        _item8 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
    {
        _item0 = item0;
        _item1 = item1;
        _item2 = item2;
        _item3 = item3;
        _item4 = item4;
        _item5 = item5;
        _item6 = item6;
        _item7 = item7;
        _item8 = item8;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(", ");
        sb.Append(Item2);
        sb.Append(", ");
        sb.Append(Item3);
        sb.Append(", ");
        sb.Append(Item4);
        sb.Append(", ");
        sb.Append(Item5);
        sb.Append(", ");
        sb.Append(Item6);
        sb.Append(", ");
        sb.Append(Item7);
        sb.Append(", ");
        sb.Append(Item8);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8 },
            1 => new object?[] { Item0, Item2, Item3, Item4, Item5, Item6, Item7, Item8 },
            2 => new object?[] { Item0, Item1, Item3, Item4, Item5, Item6, Item7, Item8 },
            3 => new object?[] { Item0, Item1, Item2, Item4, Item5, Item6, Item7, Item8 },
            4 => new object?[] { Item0, Item1, Item2, Item3, Item5, Item6, Item7, Item8 },
            5 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item6, Item7, Item8 },
            6 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item7, Item8 },
            7 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item8 },
            8 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        if (!typeof(T2).IsValueType) {
            itemType = _item2?.GetType();
            if (itemType != null && itemType != typeof(T2)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[2] = itemType;
            }
        }
        if (!typeof(T3).IsValueType) {
            itemType = _item3?.GetType();
            if (itemType != null && itemType != typeof(T3)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[3] = itemType;
            }
        }
        if (!typeof(T4).IsValueType) {
            itemType = _item4?.GetType();
            if (itemType != null && itemType != typeof(T4)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[4] = itemType;
            }
        }
        if (!typeof(T5).IsValueType) {
            itemType = _item5?.GetType();
            if (itemType != null && itemType != typeof(T5)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[5] = itemType;
            }
        }
        if (!typeof(T6).IsValueType) {
            itemType = _item6?.GetType();
            if (itemType != null && itemType != typeof(T6)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[6] = itemType;
            }
        }
        if (!typeof(T7).IsValueType) {
            itemType = _item7?.GetType();
            if (itemType != null && itemType != typeof(T7)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[7] = itemType;
            }
        }
        if (!typeof(T8).IsValueType) {
            itemType = _item8?.GetType();
            if (itemType != null && itemType != typeof(T8)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[8] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            4 => typeof(T4),
            5 => typeof(T5),
            6 => typeof(T6),
            7 => typeof(T7),
            8 => typeof(T8),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;
    public override T Get2<T>() => Item2 is T value ? value : default!;
    public override T Get3<T>() => Item3 is T value ? value : default!;
    public override T Get4<T>() => Item4 is T value ? value : default!;
    public override T Get5<T>() => Item5 is T value ? value : default!;
    public override T Get6<T>() => Item6 is T value ? value : default!;
    public override T Get7<T>() => Item7 is T value ? value : default!;
    public override T Get8<T>() => Item8 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            2 => Item2 is T value ? value : default!,
            3 => Item3 is T value ? value : default!,
            4 => Item4 is T value ? value : default!,
            5 => Item5 is T value ? value : default!,
            6 => Item6 is T value ? value : default!,
            7 => Item7 is T value ? value : default!,
            8 => Item8 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            2 => Item2,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            3 => Item3,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            4 => Item4,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            5 => Item5,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            6 => Item6,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            7 => Item7,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            8 => Item8,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            2 => Item2 is CancellationToken value ? value : default!,
            3 => Item3 is CancellationToken value ? value : default!,
            4 => Item4 is CancellationToken value ? value : default!,
            5 => Item5 is CancellationToken value ? value : default!,
            6 => Item6 is CancellationToken value ? value : default!,
            7 => Item7 is CancellationToken value ? value : default!,
            8 => Item8 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = value is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = value is T7 item7 ? item7 : default!;
            break;
        case 8:
            _item8 = value is T8 item8 ? item8 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = value is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = value is T7 item7 ? item7 : default!;
            break;
        case 8:
            _item8 = value is T8 item8 ? item8 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = item is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = item is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = item is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = item is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = item is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = item is T7 item7 ? item7 : default!;
            break;
        case 8:
            _item8 = item is T8 item8 ? item8 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
        _item2 = other.Get2<T2>();
        _item3 = other.Get3<T3>();
        _item4 = other.Get4<T4>();
        _item5 = other.Get5<T5>();
        _item6 = other.Get6<T6>();
        _item7 = other.Get7<T7>();
        _item8 = other.Get8<T8>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            1 => New(Item0, item, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            2 => New(Item0, Item1, item, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            3 => New(Item0, Item1, Item2, item, Item3, Item4, Item5, Item6, Item7, Item8),
            4 => New(Item0, Item1, Item2, Item3, item, Item4, Item5, Item6, Item7, Item8),
            5 => New(Item0, Item1, Item2, Item3, Item4, item, Item5, Item6, Item7, Item8),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, item, Item6, Item7, Item8),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, item, Item7, Item8),
            8 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, item, Item8),
            9 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => index switch {
            0 => New(item, Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            1 => New(Item0, item, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            2 => New(Item0, Item1, item, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            3 => New(Item0, Item1, Item2, item, Item3, Item4, Item5, Item6, Item7, Item8),
            4 => New(Item0, Item1, Item2, Item3, item, Item4, Item5, Item6, Item7, Item8),
            5 => New(Item0, Item1, Item2, Item3, Item4, item, Item5, Item6, Item7, Item8),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, item, Item6, Item7, Item8),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, item, Item7, Item8),
            8 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, item, Item8),
            9 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, item),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            1 => New(Item0, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            2 => New(Item0, Item1, Item3, Item4, Item5, Item6, Item7, Item8),
            3 => New(Item0, Item1, Item2, Item4, Item5, Item6, Item7, Item8),
            4 => New(Item0, Item1, Item2, Item3, Item5, Item6, Item7, Item8),
            5 => New(Item0, Item1, Item2, Item3, Item4, Item6, Item7, Item8),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item7, Item8),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item8),
            8 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 9)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[2].ParameterType != typeof(T2))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[3].ParameterType != typeof(T3))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[4].ParameterType != typeof(T4))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[5].ParameterType != typeof(T5))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[6].ParameterType != typeof(T6))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[7].ParameterType != typeof(T7))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[8].ParameterType != typeof(T8))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item2")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item3")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item4")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item5")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item6")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item7")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item8")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
        if (typeof(T2).IsValueType)
            reader.OnStruct(_item2, 2);
        else
            reader.OnObject(typeof(T2), _item2, 2);
        if (typeof(T3).IsValueType)
            reader.OnStruct(_item3, 3);
        else
            reader.OnObject(typeof(T3), _item3, 3);
        if (typeof(T4).IsValueType)
            reader.OnStruct(_item4, 4);
        else
            reader.OnObject(typeof(T4), _item4, 4);
        if (typeof(T5).IsValueType)
            reader.OnStruct(_item5, 5);
        else
            reader.OnObject(typeof(T5), _item5, 5);
        if (typeof(T6).IsValueType)
            reader.OnStruct(_item6, 6);
        else
            reader.OnObject(typeof(T6), _item6, 6);
        if (typeof(T7).IsValueType)
            reader.OnStruct(_item7, 7);
        else
            reader.OnObject(typeof(T7), _item7, 7);
        if (typeof(T8).IsValueType)
            reader.OnStruct(_item8, 8);
        else
            reader.OnObject(typeof(T8), _item8, 8);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
        if (typeof(T2).IsValueType)
            _item2 = writer.OnStruct<T2>(2);
        else
            _item2 = (T2)writer.OnObject(typeof(T2), 2)!;
        if (typeof(T3).IsValueType)
            _item3 = writer.OnStruct<T3>(3);
        else
            _item3 = (T3)writer.OnObject(typeof(T3), 3)!;
        if (typeof(T4).IsValueType)
            _item4 = writer.OnStruct<T4>(4);
        else
            _item4 = (T4)writer.OnObject(typeof(T4), 4)!;
        if (typeof(T5).IsValueType)
            _item5 = writer.OnStruct<T5>(5);
        else
            _item5 = (T5)writer.OnObject(typeof(T5), 5)!;
        if (typeof(T6).IsValueType)
            _item6 = writer.OnStruct<T6>(6);
        else
            _item6 = (T6)writer.OnObject(typeof(T6), 6)!;
        if (typeof(T7).IsValueType)
            _item7 = writer.OnStruct<T7>(7);
        else
            _item7 = (T7)writer.OnObject(typeof(T7), 7)!;
        if (typeof(T8).IsValueType)
            _item8 = writer.OnStruct<T8>(8);
        else
            _item8 = (T8)writer.OnObject(typeof(T8), 8)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8))
            return false;
        if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7))
            return false;
        if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6))
            return false;
        if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5))
            return false;
        if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4))
            return false;
        if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3))
            return false;
        if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
            return false;
        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8> vOther)
            return false;

        if (skipIndex != 8 && !EqualityComparer<T8>.Default.Equals(Item8, vOther.Item8))
            return false;
        if (skipIndex != 7 && !EqualityComparer<T7>.Default.Equals(Item7, vOther.Item7))
            return false;
        if (skipIndex != 6 && !EqualityComparer<T6>.Default.Equals(Item6, vOther.Item6))
            return false;
        if (skipIndex != 5 && !EqualityComparer<T5>.Default.Equals(Item5, vOther.Item5))
            return false;
        if (skipIndex != 4 && !EqualityComparer<T4>.Default.Equals(Item4, vOther.Item4))
            return false;
        if (skipIndex != 3 && !EqualityComparer<T3>.Default.Equals(Item3, vOther.Item3))
            return false;
        if (skipIndex != 2 && !EqualityComparer<T2>.Default.Equals(Item2, vOther.Item2))
            return false;
        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            hashCode = 397*hashCode + EqualityComparer<T2>.Default.GetHashCode(Item2!);
            hashCode = 397*hashCode + EqualityComparer<T3>.Default.GetHashCode(Item3!);
            hashCode = 397*hashCode + EqualityComparer<T4>.Default.GetHashCode(Item4!);
            hashCode = 397*hashCode + EqualityComparer<T5>.Default.GetHashCode(Item5!);
            hashCode = 397*hashCode + EqualityComparer<T6>.Default.GetHashCode(Item6!);
            hashCode = 397*hashCode + EqualityComparer<T7>.Default.GetHashCode(Item7!);
            hashCode = 397*hashCode + EqualityComparer<T8>.Default.GetHashCode(Item8!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            hashCode = 397*hashCode + (skipIndex == 2 ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2!));
            hashCode = 397*hashCode + (skipIndex == 3 ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3!));
            hashCode = 397*hashCode + (skipIndex == 4 ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4!));
            hashCode = 397*hashCode + (skipIndex == 5 ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5!));
            hashCode = 397*hashCode + (skipIndex == 6 ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6!));
            hashCode = 397*hashCode + (skipIndex == 7 ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7!));
            hashCode = 397*hashCode + (skipIndex == 8 ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8!));
            return hashCode;
        }
    }
}

public abstract record ArgumentList10 : ArgumentList
{
    protected static Type?[] CreateNonDefaultItemTypes()
        => new Type?[10];

    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public override int Length => 10;
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : ArgumentList10
{
    private T0 _item0;
    private T1 _item1;
    private T2 _item2;
    private T3 _item3;
    private T4 _item4;
    private T5 _item5;
    private T6 _item6;
    private T7 _item7;
    private T8 _item8;
    private T9 _item9;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T0 Item0 { get => _item0; init => _item0 = value; }
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T1 Item1 { get => _item1; init => _item1 = value; }
    [DataMember(Order = 2), MemoryPackOrder(2)]
    public T2 Item2 { get => _item2; init => _item2 = value; }
    [DataMember(Order = 3), MemoryPackOrder(3)]
    public T3 Item3 { get => _item3; init => _item3 = value; }
    [DataMember(Order = 4), MemoryPackOrder(4)]
    public T4 Item4 { get => _item4; init => _item4 = value; }
    [DataMember(Order = 5), MemoryPackOrder(5)]
    public T5 Item5 { get => _item5; init => _item5 = value; }
    [DataMember(Order = 6), MemoryPackOrder(6)]
    public T6 Item6 { get => _item6; init => _item6 = value; }
    [DataMember(Order = 7), MemoryPackOrder(7)]
    public T7 Item7 { get => _item7; init => _item7 = value; }
    [DataMember(Order = 8), MemoryPackOrder(8)]
    public T8 Item8 { get => _item8; init => _item8 = value; }
    [DataMember(Order = 9), MemoryPackOrder(9)]
    public T9 Item9 { get => _item9; init => _item9 = value; }

    // Constructors

    public ArgumentList()
    {
        _item0 = default!;
        _item1 = default!;
        _item2 = default!;
        _item3 = default!;
        _item4 = default!;
        _item5 = default!;
        _item6 = default!;
        _item7 = default!;
        _item8 = default!;
        _item9 = default!;
    }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
    {
        _item0 = item0;
        _item1 = item1;
        _item2 = item2;
        _item3 = item3;
        _item4 = item4;
        _item5 = item5;
        _item6 = item6;
        _item7 = item7;
        _item8 = item8;
        _item9 = item9;
    }

    // ToString & ToArray

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('(');
        sb.Append(Item0);
        sb.Append(", ");
        sb.Append(Item1);
        sb.Append(", ");
        sb.Append(Item2);
        sb.Append(", ");
        sb.Append(Item3);
        sb.Append(", ");
        sb.Append(Item4);
        sb.Append(", ");
        sb.Append(Item5);
        sb.Append(", ");
        sb.Append(Item6);
        sb.Append(", ");
        sb.Append(Item7);
        sb.Append(", ");
        sb.Append(Item8);
        sb.Append(", ");
        sb.Append(Item9);
        sb.Append(')');
        return sb.ToString();
    }

    public override object?[] ToArray()
        => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9 };

    public override object?[] ToArray(int skipIndex)
        => skipIndex switch {
            0 => new object?[] { Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9 },
            1 => new object?[] { Item0, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9 },
            2 => new object?[] { Item0, Item1, Item3, Item4, Item5, Item6, Item7, Item8, Item9 },
            3 => new object?[] { Item0, Item1, Item2, Item4, Item5, Item6, Item7, Item8, Item9 },
            4 => new object?[] { Item0, Item1, Item2, Item3, Item5, Item6, Item7, Item8, Item9 },
            5 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item6, Item7, Item8, Item9 },
            6 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item7, Item8, Item9 },
            7 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item8, Item9 },
            8 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item9 },
            9 => new object?[] { Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8 },
            _ => throw new ArgumentOutOfRangeException(nameof(skipIndex))
        };

    // GetNonDefaultItemTypes

    public override Type?[]? GetNonDefaultItemTypes() {
        var itemTypes = (Type?[]?)null;
        Type? itemType;
        if (!typeof(T0).IsValueType) {
            itemType = _item0?.GetType();
            if (itemType != null && itemType != typeof(T0)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[0] = itemType;
            }
        }
        if (!typeof(T1).IsValueType) {
            itemType = _item1?.GetType();
            if (itemType != null && itemType != typeof(T1)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[1] = itemType;
            }
        }
        if (!typeof(T2).IsValueType) {
            itemType = _item2?.GetType();
            if (itemType != null && itemType != typeof(T2)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[2] = itemType;
            }
        }
        if (!typeof(T3).IsValueType) {
            itemType = _item3?.GetType();
            if (itemType != null && itemType != typeof(T3)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[3] = itemType;
            }
        }
        if (!typeof(T4).IsValueType) {
            itemType = _item4?.GetType();
            if (itemType != null && itemType != typeof(T4)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[4] = itemType;
            }
        }
        if (!typeof(T5).IsValueType) {
            itemType = _item5?.GetType();
            if (itemType != null && itemType != typeof(T5)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[5] = itemType;
            }
        }
        if (!typeof(T6).IsValueType) {
            itemType = _item6?.GetType();
            if (itemType != null && itemType != typeof(T6)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[6] = itemType;
            }
        }
        if (!typeof(T7).IsValueType) {
            itemType = _item7?.GetType();
            if (itemType != null && itemType != typeof(T7)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[7] = itemType;
            }
        }
        if (!typeof(T8).IsValueType) {
            itemType = _item8?.GetType();
            if (itemType != null && itemType != typeof(T8)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[8] = itemType;
            }
        }
        if (!typeof(T9).IsValueType) {
            itemType = _item9?.GetType();
            if (itemType != null && itemType != typeof(T9)) {
                itemTypes ??= CreateNonDefaultItemTypes();
                itemTypes[9] = itemType;
            }
        }
        return itemTypes;
    }

    // GetType

    public override Type? GetType(int index)
        => index switch {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            4 => typeof(T4),
            5 => typeof(T5),
            6 => typeof(T6),
            7 => typeof(T7),
            8 => typeof(T8),
            9 => typeof(T9),
            _ => null,
        };

    // Get

    public override T Get0<T>() => Item0 is T value ? value : default!;
    public override T Get1<T>() => Item1 is T value ? value : default!;
    public override T Get2<T>() => Item2 is T value ? value : default!;
    public override T Get3<T>() => Item3 is T value ? value : default!;
    public override T Get4<T>() => Item4 is T value ? value : default!;
    public override T Get5<T>() => Item5 is T value ? value : default!;
    public override T Get6<T>() => Item6 is T value ? value : default!;
    public override T Get7<T>() => Item7 is T value ? value : default!;
    public override T Get8<T>() => Item8 is T value ? value : default!;
    public override T Get9<T>() => Item9 is T value ? value : default!;

    public override T Get<T>(int index)
        => index switch {
            0 => Item0 is T value ? value : default!,
            1 => Item1 is T value ? value : default!,
            2 => Item2 is T value ? value : default!,
            3 => Item3 is T value ? value : default!,
            4 => Item4 is T value ? value : default!,
            5 => Item5 is T value ? value : default!,
            6 => Item6 is T value ? value : default!,
            7 => Item7 is T value ? value : default!,
            8 => Item8 is T value ? value : default!,
            9 => Item9 is T value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override object? GetUntyped(int index)
        => index switch {
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            0 => Item0,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            1 => Item1,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            2 => Item2,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            3 => Item3,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            4 => Item4,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            5 => Item5,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            6 => Item6,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            7 => Item7,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            8 => Item8,
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            9 => Item9,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public override CancellationToken GetCancellationToken(int index)
        => index switch {
            0 => Item0 is CancellationToken value ? value : default!,
            1 => Item1 is CancellationToken value ? value : default!,
            2 => Item2 is CancellationToken value ? value : default!,
            3 => Item3 is CancellationToken value ? value : default!,
            4 => Item4 is CancellationToken value ? value : default!,
            5 => Item5 is CancellationToken value ? value : default!,
            6 => Item6 is CancellationToken value ? value : default!,
            7 => Item7 is CancellationToken value ? value : default!,
            8 => Item8 is CancellationToken value ? value : default!,
            9 => Item9 is CancellationToken value ? value : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // Set

    public override void Set<T>(int index, T value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = value is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = value is T7 item7 ? item7 : default!;
            break;
        case 8:
            _item8 = value is T8 item8 ? item8 : default!;
            break;
        case 9:
            _item9 = value is T9 item9 ? item9 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetUntyped(int index, object? value)
    {
        switch (index) {
        case 0:
            _item0 = value is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = value is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = value is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = value is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = value is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = value is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = value is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = value is T7 item7 ? item7 : default!;
            break;
        case 8:
            _item8 = value is T8 item8 ? item8 : default!;
            break;
        case 9:
            _item9 = value is T9 item9 ? item9 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public override void SetCancellationToken(int index, CancellationToken item)
    {
        switch (index) {
        case 0:
            _item0 = item is T0 item0 ? item0 : default!;
            break;
        case 1:
            _item1 = item is T1 item1 ? item1 : default!;
            break;
        case 2:
            _item2 = item is T2 item2 ? item2 : default!;
            break;
        case 3:
            _item3 = item is T3 item3 ? item3 : default!;
            break;
        case 4:
            _item4 = item is T4 item4 ? item4 : default!;
            break;
        case 5:
            _item5 = item is T5 item5 ? item5 : default!;
            break;
        case 6:
            _item6 = item is T6 item6 ? item6 : default!;
            break;
        case 7:
            _item7 = item is T7 item7 ? item7 : default!;
            break;
        case 8:
            _item8 = item is T8 item8 ? item8 : default!;
            break;
        case 9:
            _item9 = item is T9 item9 ? item9 : default!;
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // SetFrom

    public override void SetFrom(ArgumentList other)
    {
        _item0 = other.Get0<T0>();
        _item1 = other.Get1<T1>();
        _item2 = other.Get2<T2>();
        _item3 = other.Get3<T3>();
        _item4 = other.Get4<T4>();
        _item5 = other.Get5<T5>();
        _item6 = other.Get6<T6>();
        _item7 = other.Get7<T7>();
        _item8 = other.Get8<T8>();
        _item9 = other.Get9<T9>();
    }

    // Insert

    public override ArgumentList Insert<T>(int index, T item)
        => throw new ArgumentOutOfRangeException(nameof(index));

    public override ArgumentList InsertCancellationToken(int index, CancellationToken item)
        => throw new ArgumentOutOfRangeException(nameof(index));

    // Remove

    public override ArgumentList Remove(int index)
        => index switch {
            0 => New(Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9),
            1 => New(Item0, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9),
            2 => New(Item0, Item1, Item3, Item4, Item5, Item6, Item7, Item8, Item9),
            3 => New(Item0, Item1, Item2, Item4, Item5, Item6, Item7, Item8, Item9),
            4 => New(Item0, Item1, Item2, Item3, Item5, Item6, Item7, Item8, Item9),
            5 => New(Item0, Item1, Item2, Item3, Item4, Item6, Item7, Item8, Item9),
            6 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item7, Item8, Item9),
            7 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item8, Item9),
            8 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item9),
            9 => New(Item0, Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    // GetInvoker

    public override Func<object, ArgumentList, object?> GetInvoker(MethodInfo method)
        => InvokerCache.GetOrAdd((GetType(), method), static key => {
            var (listType, method1) = key;
            var parameters = method1.GetParameters();
            if (parameters.Length != 10)
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[0].ParameterType != typeof(T0))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[1].ParameterType != typeof(T1))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[2].ParameterType != typeof(T2))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[3].ParameterType != typeof(T3))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[4].ParameterType != typeof(T4))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[5].ParameterType != typeof(T5))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[6].ParameterType != typeof(T6))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[7].ParameterType != typeof(T7))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[8].ParameterType != typeof(T8))
                throw new ArgumentOutOfRangeException(nameof(method));
            if (parameters[9].ParameterType != typeof(T9))
                throw new ArgumentOutOfRangeException(nameof(method));

            var declaringType = method1.DeclaringType!;
            var m = new DynamicMethod("_Invoke",
                typeof(object),
                new [] { typeof(object), typeof(ArgumentList) },
                true);
            var il = m.GetILGenerator();

            // Cast ArgumentList to its actual type
            il.DeclareLocal(listType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, listType);
            il.Emit(OpCodes.Stloc_0);

            // Unbox target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(declaringType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaringType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item0")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item1")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item2")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item3")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item4")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item5")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item6")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item7")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item8")!.GetGetMethod()!);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, listType.GetProperty("Item9")!.GetGetMethod()!);

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
    {
        if (typeof(T0).IsValueType)
            reader.OnStruct(_item0, 0);
        else
            reader.OnObject(typeof(T0), _item0, 0);
        if (typeof(T1).IsValueType)
            reader.OnStruct(_item1, 1);
        else
            reader.OnObject(typeof(T1), _item1, 1);
        if (typeof(T2).IsValueType)
            reader.OnStruct(_item2, 2);
        else
            reader.OnObject(typeof(T2), _item2, 2);
        if (typeof(T3).IsValueType)
            reader.OnStruct(_item3, 3);
        else
            reader.OnObject(typeof(T3), _item3, 3);
        if (typeof(T4).IsValueType)
            reader.OnStruct(_item4, 4);
        else
            reader.OnObject(typeof(T4), _item4, 4);
        if (typeof(T5).IsValueType)
            reader.OnStruct(_item5, 5);
        else
            reader.OnObject(typeof(T5), _item5, 5);
        if (typeof(T6).IsValueType)
            reader.OnStruct(_item6, 6);
        else
            reader.OnObject(typeof(T6), _item6, 6);
        if (typeof(T7).IsValueType)
            reader.OnStruct(_item7, 7);
        else
            reader.OnObject(typeof(T7), _item7, 7);
        if (typeof(T8).IsValueType)
            reader.OnStruct(_item8, 8);
        else
            reader.OnObject(typeof(T8), _item8, 8);
        if (typeof(T9).IsValueType)
            reader.OnStruct(_item9, 9);
        else
            reader.OnObject(typeof(T9), _item9, 9);
    }

    public override void Write(ArgumentListWriter writer)
    {
        if (typeof(T0).IsValueType)
            _item0 = writer.OnStruct<T0>(0);
        else
            _item0 = (T0)writer.OnObject(typeof(T0), 0)!;
        if (typeof(T1).IsValueType)
            _item1 = writer.OnStruct<T1>(1);
        else
            _item1 = (T1)writer.OnObject(typeof(T1), 1)!;
        if (typeof(T2).IsValueType)
            _item2 = writer.OnStruct<T2>(2);
        else
            _item2 = (T2)writer.OnObject(typeof(T2), 2)!;
        if (typeof(T3).IsValueType)
            _item3 = writer.OnStruct<T3>(3);
        else
            _item3 = (T3)writer.OnObject(typeof(T3), 3)!;
        if (typeof(T4).IsValueType)
            _item4 = writer.OnStruct<T4>(4);
        else
            _item4 = (T4)writer.OnObject(typeof(T4), 4)!;
        if (typeof(T5).IsValueType)
            _item5 = writer.OnStruct<T5>(5);
        else
            _item5 = (T5)writer.OnObject(typeof(T5), 5)!;
        if (typeof(T6).IsValueType)
            _item6 = writer.OnStruct<T6>(6);
        else
            _item6 = (T6)writer.OnObject(typeof(T6), 6)!;
        if (typeof(T7).IsValueType)
            _item7 = writer.OnStruct<T7>(7);
        else
            _item7 = (T7)writer.OnObject(typeof(T7), 7)!;
        if (typeof(T8).IsValueType)
            _item8 = writer.OnStruct<T8>(8);
        else
            _item8 = (T8)writer.OnObject(typeof(T8), 8)!;
        if (typeof(T9).IsValueType)
            _item9 = writer.OnStruct<T9>(9);
        else
            _item9 = (T9)writer.OnObject(typeof(T9), 9)!;
    }

    // Equality

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? other)
    {
        if (other == null)
            return false;

        if (!EqualityComparer<T9>.Default.Equals(Item9, other.Item9))
            return false;
        if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8))
            return false;
        if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7))
            return false;
        if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6))
            return false;
        if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5))
            return false;
        if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4))
            return false;
        if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3))
            return false;
        if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
            return false;
        if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
            return false;
        if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0))
            return false;
        return true;
    }

    public override bool Equals(ArgumentList? other, int skipIndex)
    {
        if (other is not ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> vOther)
            return false;

        if (skipIndex != 9 && !EqualityComparer<T9>.Default.Equals(Item9, vOther.Item9))
            return false;
        if (skipIndex != 8 && !EqualityComparer<T8>.Default.Equals(Item8, vOther.Item8))
            return false;
        if (skipIndex != 7 && !EqualityComparer<T7>.Default.Equals(Item7, vOther.Item7))
            return false;
        if (skipIndex != 6 && !EqualityComparer<T6>.Default.Equals(Item6, vOther.Item6))
            return false;
        if (skipIndex != 5 && !EqualityComparer<T5>.Default.Equals(Item5, vOther.Item5))
            return false;
        if (skipIndex != 4 && !EqualityComparer<T4>.Default.Equals(Item4, vOther.Item4))
            return false;
        if (skipIndex != 3 && !EqualityComparer<T3>.Default.Equals(Item3, vOther.Item3))
            return false;
        if (skipIndex != 2 && !EqualityComparer<T2>.Default.Equals(Item2, vOther.Item2))
            return false;
        if (skipIndex != 1 && !EqualityComparer<T1>.Default.Equals(Item1, vOther.Item1))
            return false;
        if (skipIndex != 0 && !EqualityComparer<T0>.Default.Equals(Item0, vOther.Item0))
            return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + EqualityComparer<T1>.Default.GetHashCode(Item1!);
            hashCode = 397*hashCode + EqualityComparer<T2>.Default.GetHashCode(Item2!);
            hashCode = 397*hashCode + EqualityComparer<T3>.Default.GetHashCode(Item3!);
            hashCode = 397*hashCode + EqualityComparer<T4>.Default.GetHashCode(Item4!);
            hashCode = 397*hashCode + EqualityComparer<T5>.Default.GetHashCode(Item5!);
            hashCode = 397*hashCode + EqualityComparer<T6>.Default.GetHashCode(Item6!);
            hashCode = 397*hashCode + EqualityComparer<T7>.Default.GetHashCode(Item7!);
            hashCode = 397*hashCode + EqualityComparer<T8>.Default.GetHashCode(Item8!);
            hashCode = 397*hashCode + EqualityComparer<T9>.Default.GetHashCode(Item9!);
            return hashCode;
        }
    }

    public override int GetHashCode(int skipIndex)
    {
        unchecked {
            var hashCode = skipIndex == 0 ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0!);
            hashCode = 397*hashCode + (skipIndex == 1 ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1!));
            hashCode = 397*hashCode + (skipIndex == 2 ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2!));
            hashCode = 397*hashCode + (skipIndex == 3 ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3!));
            hashCode = 397*hashCode + (skipIndex == 4 ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4!));
            hashCode = 397*hashCode + (skipIndex == 5 ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5!));
            hashCode = 397*hashCode + (skipIndex == 6 ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6!));
            hashCode = 397*hashCode + (skipIndex == 7 ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7!));
            hashCode = 397*hashCode + (skipIndex == 8 ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8!));
            hashCode = 397*hashCode + (skipIndex == 9 ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9!));
            return hashCode;
        }
    }
}

