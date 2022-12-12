// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeConstructorOrDestructorBody
namespace Stl.Interception;

public partial record ArgumentList
{
    public readonly ArgumentList Empty = new ();

    public virtual int Length => 0;

    public object? this[int index]
    {
      get {
        if (index < 0 || index >= Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return GetItem(index);
      }
    }

    protected virtual object? GetItem(int index)
        => throw new NotSupportedException();

    public static ArgumentList<T0> New<T0>(T0 item0)
        => new (item0);

    public static ArgumentList<T0, T1> New<T0, T1>(T0 item0, T1 item1)
        => new (item0, item1);

    public static ArgumentList<T0, T1, T2> New<T0, T1, T2>(T0 item0, T1 item1, T2 item2)
        => new (item0, item1, item2);

    public static ArgumentList<T0, T1, T2, T3> New<T0, T1, T2, T3>(T0 item0, T1 item1, T2 item2, T3 item3)
        => new (item0, item1, item2, item3);

    public static ArgumentList<T0, T1, T2, T3, T4> New<T0, T1, T2, T3, T4>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4)
        => new (item0, item1, item2, item3, item4);

    public static ArgumentList<T0, T1, T2, T3, T4, T5> New<T0, T1, T2, T3, T4, T5>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        => new (item0, item1, item2, item3, item4, item5);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6> New<T0, T1, T2, T3, T4, T5, T6>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        => new (item0, item1, item2, item3, item4, item5, item6);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7> New<T0, T1, T2, T3, T4, T5, T6, T7>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        => new (item0, item1, item2, item3, item4, item5, item6, item7);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8> New<T0, T1, T2, T3, T4, T5, T6, T7, T8>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8, item9);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14);

    public static ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15)
        => new (item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15);

    protected ArgumentList() {}
}

public sealed record ArgumentList<T0> : ArgumentList, IEquatable<ArgumentList<T0>>
{
    public override int Length => 1;

    public T0 Item0 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 1)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 1)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            return hashCode;
        }
    }

    public ArgumentList(T0 item0)
    {
        Item0 = item0;
    }
}

public sealed record ArgumentList<T0, T1> : ArgumentList, IEquatable<ArgumentList<T0, T1>>
{
    public override int Length => 2;

    public T0 Item0 { get; }
    public T1 Item1 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1)
    {
        Item0 = item0;
        Item1 = item1;
    }
}

public sealed record ArgumentList<T0, T1, T2> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2>>
{
    public override int Length => 3;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 3)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 3)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3>>
{
    public override int Length => 4;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 4)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 4)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4>>
{
    public override int Length => 5;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 5)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 5)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5>>
{
    public override int Length => 6;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 6)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 6)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6>>
{
    public override int Length => 7;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 7)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 7)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7>>
{
    public override int Length => 8;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 8)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 8)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8>>
{
    public override int Length => 9;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            8 => Item8,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        if (Item8 is not CancellationToken && !EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            hashCode = (hashCode * 397) + (Item8 is CancellationToken || Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 9)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        if (equalDelegates[8] is Func<T8, T8, bool> func8) {
            if (!func8.Invoke(Item8, other.Item8))
                return false;
        }
        else if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 9)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            if (getHashCodeDelegates[8] is Func<T8, int> func8)
                hashCode = (hashCode * 397) + func8(Item8);
            else
                hashCode = (hashCode * 397) + (Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>
{
    public override int Length => 10;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }
    public T9 Item9 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            8 => Item8,
            9 => Item9,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        if (Item8 is not CancellationToken && !EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) return false;
        if (Item9 is not CancellationToken && !EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            hashCode = (hashCode * 397) + (Item8 is CancellationToken || Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));
            hashCode = (hashCode * 397) + (Item9 is CancellationToken || Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 10)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        if (equalDelegates[8] is Func<T8, T8, bool> func8) {
            if (!func8.Invoke(Item8, other.Item8))
                return false;
        }
        else if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) {
            return false;
        }

        if (equalDelegates[9] is Func<T9, T9, bool> func9) {
            if (!func9.Invoke(Item9, other.Item9))
                return false;
        }
        else if (!EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 10)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            if (getHashCodeDelegates[8] is Func<T8, int> func8)
                hashCode = (hashCode * 397) + func8(Item8);
            else
                hashCode = (hashCode * 397) + (Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));

            if (getHashCodeDelegates[9] is Func<T9, int> func9)
                hashCode = (hashCode * 397) + func9(Item9);
            else
                hashCode = (hashCode * 397) + (Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>
{
    public override int Length => 11;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }
    public T9 Item9 { get; }
    public T10 Item10 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            8 => Item8,
            9 => Item9,
            10 => Item10,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        if (Item8 is not CancellationToken && !EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) return false;
        if (Item9 is not CancellationToken && !EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) return false;
        if (Item10 is not CancellationToken && !EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            hashCode = (hashCode * 397) + (Item8 is CancellationToken || Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));
            hashCode = (hashCode * 397) + (Item9 is CancellationToken || Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));
            hashCode = (hashCode * 397) + (Item10 is CancellationToken || Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 11)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        if (equalDelegates[8] is Func<T8, T8, bool> func8) {
            if (!func8.Invoke(Item8, other.Item8))
                return false;
        }
        else if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) {
            return false;
        }

        if (equalDelegates[9] is Func<T9, T9, bool> func9) {
            if (!func9.Invoke(Item9, other.Item9))
                return false;
        }
        else if (!EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) {
            return false;
        }

        if (equalDelegates[10] is Func<T10, T10, bool> func10) {
            if (!func10.Invoke(Item10, other.Item10))
                return false;
        }
        else if (!EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 11)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            if (getHashCodeDelegates[8] is Func<T8, int> func8)
                hashCode = (hashCode * 397) + func8(Item8);
            else
                hashCode = (hashCode * 397) + (Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));

            if (getHashCodeDelegates[9] is Func<T9, int> func9)
                hashCode = (hashCode * 397) + func9(Item9);
            else
                hashCode = (hashCode * 397) + (Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));

            if (getHashCodeDelegates[10] is Func<T10, int> func10)
                hashCode = (hashCode * 397) + func10(Item10);
            else
                hashCode = (hashCode * 397) + (Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>
{
    public override int Length => 12;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }
    public T9 Item9 { get; }
    public T10 Item10 { get; }
    public T11 Item11 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            8 => Item8,
            9 => Item9,
            10 => Item10,
            11 => Item11,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        if (Item8 is not CancellationToken && !EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) return false;
        if (Item9 is not CancellationToken && !EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) return false;
        if (Item10 is not CancellationToken && !EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) return false;
        if (Item11 is not CancellationToken && !EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            hashCode = (hashCode * 397) + (Item8 is CancellationToken || Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));
            hashCode = (hashCode * 397) + (Item9 is CancellationToken || Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));
            hashCode = (hashCode * 397) + (Item10 is CancellationToken || Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));
            hashCode = (hashCode * 397) + (Item11 is CancellationToken || Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 12)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        if (equalDelegates[8] is Func<T8, T8, bool> func8) {
            if (!func8.Invoke(Item8, other.Item8))
                return false;
        }
        else if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) {
            return false;
        }

        if (equalDelegates[9] is Func<T9, T9, bool> func9) {
            if (!func9.Invoke(Item9, other.Item9))
                return false;
        }
        else if (!EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) {
            return false;
        }

        if (equalDelegates[10] is Func<T10, T10, bool> func10) {
            if (!func10.Invoke(Item10, other.Item10))
                return false;
        }
        else if (!EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) {
            return false;
        }

        if (equalDelegates[11] is Func<T11, T11, bool> func11) {
            if (!func11.Invoke(Item11, other.Item11))
                return false;
        }
        else if (!EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 12)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            if (getHashCodeDelegates[8] is Func<T8, int> func8)
                hashCode = (hashCode * 397) + func8(Item8);
            else
                hashCode = (hashCode * 397) + (Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));

            if (getHashCodeDelegates[9] is Func<T9, int> func9)
                hashCode = (hashCode * 397) + func9(Item9);
            else
                hashCode = (hashCode * 397) + (Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));

            if (getHashCodeDelegates[10] is Func<T10, int> func10)
                hashCode = (hashCode * 397) + func10(Item10);
            else
                hashCode = (hashCode * 397) + (Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));

            if (getHashCodeDelegates[11] is Func<T11, int> func11)
                hashCode = (hashCode * 397) + func11(Item11);
            else
                hashCode = (hashCode * 397) + (Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
        Item11 = item11;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>
{
    public override int Length => 13;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }
    public T9 Item9 { get; }
    public T10 Item10 { get; }
    public T11 Item11 { get; }
    public T12 Item12 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            8 => Item8,
            9 => Item9,
            10 => Item10,
            11 => Item11,
            12 => Item12,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        if (Item8 is not CancellationToken && !EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) return false;
        if (Item9 is not CancellationToken && !EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) return false;
        if (Item10 is not CancellationToken && !EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) return false;
        if (Item11 is not CancellationToken && !EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) return false;
        if (Item12 is not CancellationToken && !EqualityComparer<T12>.Default.Equals(Item12, other.Item12)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            hashCode = (hashCode * 397) + (Item8 is CancellationToken || Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));
            hashCode = (hashCode * 397) + (Item9 is CancellationToken || Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));
            hashCode = (hashCode * 397) + (Item10 is CancellationToken || Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));
            hashCode = (hashCode * 397) + (Item11 is CancellationToken || Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));
            hashCode = (hashCode * 397) + (Item12 is CancellationToken || Item12 is null ? 0 : EqualityComparer<T12>.Default.GetHashCode(Item12));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 13)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        if (equalDelegates[8] is Func<T8, T8, bool> func8) {
            if (!func8.Invoke(Item8, other.Item8))
                return false;
        }
        else if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) {
            return false;
        }

        if (equalDelegates[9] is Func<T9, T9, bool> func9) {
            if (!func9.Invoke(Item9, other.Item9))
                return false;
        }
        else if (!EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) {
            return false;
        }

        if (equalDelegates[10] is Func<T10, T10, bool> func10) {
            if (!func10.Invoke(Item10, other.Item10))
                return false;
        }
        else if (!EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) {
            return false;
        }

        if (equalDelegates[11] is Func<T11, T11, bool> func11) {
            if (!func11.Invoke(Item11, other.Item11))
                return false;
        }
        else if (!EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) {
            return false;
        }

        if (equalDelegates[12] is Func<T12, T12, bool> func12) {
            if (!func12.Invoke(Item12, other.Item12))
                return false;
        }
        else if (!EqualityComparer<T12>.Default.Equals(Item12, other.Item12)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 13)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            if (getHashCodeDelegates[8] is Func<T8, int> func8)
                hashCode = (hashCode * 397) + func8(Item8);
            else
                hashCode = (hashCode * 397) + (Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));

            if (getHashCodeDelegates[9] is Func<T9, int> func9)
                hashCode = (hashCode * 397) + func9(Item9);
            else
                hashCode = (hashCode * 397) + (Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));

            if (getHashCodeDelegates[10] is Func<T10, int> func10)
                hashCode = (hashCode * 397) + func10(Item10);
            else
                hashCode = (hashCode * 397) + (Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));

            if (getHashCodeDelegates[11] is Func<T11, int> func11)
                hashCode = (hashCode * 397) + func11(Item11);
            else
                hashCode = (hashCode * 397) + (Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));

            if (getHashCodeDelegates[12] is Func<T12, int> func12)
                hashCode = (hashCode * 397) + func12(Item12);
            else
                hashCode = (hashCode * 397) + (Item12 is null ? 0 : EqualityComparer<T12>.Default.GetHashCode(Item12));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
        Item11 = item11;
        Item12 = item12;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>
{
    public override int Length => 14;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }
    public T9 Item9 { get; }
    public T10 Item10 { get; }
    public T11 Item11 { get; }
    public T12 Item12 { get; }
    public T13 Item13 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            8 => Item8,
            9 => Item9,
            10 => Item10,
            11 => Item11,
            12 => Item12,
            13 => Item13,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        if (Item8 is not CancellationToken && !EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) return false;
        if (Item9 is not CancellationToken && !EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) return false;
        if (Item10 is not CancellationToken && !EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) return false;
        if (Item11 is not CancellationToken && !EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) return false;
        if (Item12 is not CancellationToken && !EqualityComparer<T12>.Default.Equals(Item12, other.Item12)) return false;
        if (Item13 is not CancellationToken && !EqualityComparer<T13>.Default.Equals(Item13, other.Item13)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            hashCode = (hashCode * 397) + (Item8 is CancellationToken || Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));
            hashCode = (hashCode * 397) + (Item9 is CancellationToken || Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));
            hashCode = (hashCode * 397) + (Item10 is CancellationToken || Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));
            hashCode = (hashCode * 397) + (Item11 is CancellationToken || Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));
            hashCode = (hashCode * 397) + (Item12 is CancellationToken || Item12 is null ? 0 : EqualityComparer<T12>.Default.GetHashCode(Item12));
            hashCode = (hashCode * 397) + (Item13 is CancellationToken || Item13 is null ? 0 : EqualityComparer<T13>.Default.GetHashCode(Item13));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 14)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        if (equalDelegates[8] is Func<T8, T8, bool> func8) {
            if (!func8.Invoke(Item8, other.Item8))
                return false;
        }
        else if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) {
            return false;
        }

        if (equalDelegates[9] is Func<T9, T9, bool> func9) {
            if (!func9.Invoke(Item9, other.Item9))
                return false;
        }
        else if (!EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) {
            return false;
        }

        if (equalDelegates[10] is Func<T10, T10, bool> func10) {
            if (!func10.Invoke(Item10, other.Item10))
                return false;
        }
        else if (!EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) {
            return false;
        }

        if (equalDelegates[11] is Func<T11, T11, bool> func11) {
            if (!func11.Invoke(Item11, other.Item11))
                return false;
        }
        else if (!EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) {
            return false;
        }

        if (equalDelegates[12] is Func<T12, T12, bool> func12) {
            if (!func12.Invoke(Item12, other.Item12))
                return false;
        }
        else if (!EqualityComparer<T12>.Default.Equals(Item12, other.Item12)) {
            return false;
        }

        if (equalDelegates[13] is Func<T13, T13, bool> func13) {
            if (!func13.Invoke(Item13, other.Item13))
                return false;
        }
        else if (!EqualityComparer<T13>.Default.Equals(Item13, other.Item13)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 14)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            if (getHashCodeDelegates[8] is Func<T8, int> func8)
                hashCode = (hashCode * 397) + func8(Item8);
            else
                hashCode = (hashCode * 397) + (Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));

            if (getHashCodeDelegates[9] is Func<T9, int> func9)
                hashCode = (hashCode * 397) + func9(Item9);
            else
                hashCode = (hashCode * 397) + (Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));

            if (getHashCodeDelegates[10] is Func<T10, int> func10)
                hashCode = (hashCode * 397) + func10(Item10);
            else
                hashCode = (hashCode * 397) + (Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));

            if (getHashCodeDelegates[11] is Func<T11, int> func11)
                hashCode = (hashCode * 397) + func11(Item11);
            else
                hashCode = (hashCode * 397) + (Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));

            if (getHashCodeDelegates[12] is Func<T12, int> func12)
                hashCode = (hashCode * 397) + func12(Item12);
            else
                hashCode = (hashCode * 397) + (Item12 is null ? 0 : EqualityComparer<T12>.Default.GetHashCode(Item12));

            if (getHashCodeDelegates[13] is Func<T13, int> func13)
                hashCode = (hashCode * 397) + func13(Item13);
            else
                hashCode = (hashCode * 397) + (Item13 is null ? 0 : EqualityComparer<T13>.Default.GetHashCode(Item13));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
        Item11 = item11;
        Item12 = item12;
        Item13 = item13;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>
{
    public override int Length => 15;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }
    public T9 Item9 { get; }
    public T10 Item10 { get; }
    public T11 Item11 { get; }
    public T12 Item12 { get; }
    public T13 Item13 { get; }
    public T14 Item14 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            8 => Item8,
            9 => Item9,
            10 => Item10,
            11 => Item11,
            12 => Item12,
            13 => Item13,
            14 => Item14,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        if (Item8 is not CancellationToken && !EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) return false;
        if (Item9 is not CancellationToken && !EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) return false;
        if (Item10 is not CancellationToken && !EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) return false;
        if (Item11 is not CancellationToken && !EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) return false;
        if (Item12 is not CancellationToken && !EqualityComparer<T12>.Default.Equals(Item12, other.Item12)) return false;
        if (Item13 is not CancellationToken && !EqualityComparer<T13>.Default.Equals(Item13, other.Item13)) return false;
        if (Item14 is not CancellationToken && !EqualityComparer<T14>.Default.Equals(Item14, other.Item14)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            hashCode = (hashCode * 397) + (Item8 is CancellationToken || Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));
            hashCode = (hashCode * 397) + (Item9 is CancellationToken || Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));
            hashCode = (hashCode * 397) + (Item10 is CancellationToken || Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));
            hashCode = (hashCode * 397) + (Item11 is CancellationToken || Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));
            hashCode = (hashCode * 397) + (Item12 is CancellationToken || Item12 is null ? 0 : EqualityComparer<T12>.Default.GetHashCode(Item12));
            hashCode = (hashCode * 397) + (Item13 is CancellationToken || Item13 is null ? 0 : EqualityComparer<T13>.Default.GetHashCode(Item13));
            hashCode = (hashCode * 397) + (Item14 is CancellationToken || Item14 is null ? 0 : EqualityComparer<T14>.Default.GetHashCode(Item14));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 15)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        if (equalDelegates[8] is Func<T8, T8, bool> func8) {
            if (!func8.Invoke(Item8, other.Item8))
                return false;
        }
        else if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) {
            return false;
        }

        if (equalDelegates[9] is Func<T9, T9, bool> func9) {
            if (!func9.Invoke(Item9, other.Item9))
                return false;
        }
        else if (!EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) {
            return false;
        }

        if (equalDelegates[10] is Func<T10, T10, bool> func10) {
            if (!func10.Invoke(Item10, other.Item10))
                return false;
        }
        else if (!EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) {
            return false;
        }

        if (equalDelegates[11] is Func<T11, T11, bool> func11) {
            if (!func11.Invoke(Item11, other.Item11))
                return false;
        }
        else if (!EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) {
            return false;
        }

        if (equalDelegates[12] is Func<T12, T12, bool> func12) {
            if (!func12.Invoke(Item12, other.Item12))
                return false;
        }
        else if (!EqualityComparer<T12>.Default.Equals(Item12, other.Item12)) {
            return false;
        }

        if (equalDelegates[13] is Func<T13, T13, bool> func13) {
            if (!func13.Invoke(Item13, other.Item13))
                return false;
        }
        else if (!EqualityComparer<T13>.Default.Equals(Item13, other.Item13)) {
            return false;
        }

        if (equalDelegates[14] is Func<T14, T14, bool> func14) {
            if (!func14.Invoke(Item14, other.Item14))
                return false;
        }
        else if (!EqualityComparer<T14>.Default.Equals(Item14, other.Item14)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 15)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            if (getHashCodeDelegates[8] is Func<T8, int> func8)
                hashCode = (hashCode * 397) + func8(Item8);
            else
                hashCode = (hashCode * 397) + (Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));

            if (getHashCodeDelegates[9] is Func<T9, int> func9)
                hashCode = (hashCode * 397) + func9(Item9);
            else
                hashCode = (hashCode * 397) + (Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));

            if (getHashCodeDelegates[10] is Func<T10, int> func10)
                hashCode = (hashCode * 397) + func10(Item10);
            else
                hashCode = (hashCode * 397) + (Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));

            if (getHashCodeDelegates[11] is Func<T11, int> func11)
                hashCode = (hashCode * 397) + func11(Item11);
            else
                hashCode = (hashCode * 397) + (Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));

            if (getHashCodeDelegates[12] is Func<T12, int> func12)
                hashCode = (hashCode * 397) + func12(Item12);
            else
                hashCode = (hashCode * 397) + (Item12 is null ? 0 : EqualityComparer<T12>.Default.GetHashCode(Item12));

            if (getHashCodeDelegates[13] is Func<T13, int> func13)
                hashCode = (hashCode * 397) + func13(Item13);
            else
                hashCode = (hashCode * 397) + (Item13 is null ? 0 : EqualityComparer<T13>.Default.GetHashCode(Item13));

            if (getHashCodeDelegates[14] is Func<T14, int> func14)
                hashCode = (hashCode * 397) + func14(Item14);
            else
                hashCode = (hashCode * 397) + (Item14 is null ? 0 : EqualityComparer<T14>.Default.GetHashCode(Item14));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
        Item11 = item11;
        Item12 = item12;
        Item13 = item13;
        Item14 = item14;
    }
}

public sealed record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : ArgumentList, IEquatable<ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>
{
    public override int Length => 16;

    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }
    public T9 Item9 { get; }
    public T10 Item10 { get; }
    public T11 Item11 { get; }
    public T12 Item12 { get; }
    public T13 Item13 { get; }
    public T14 Item14 { get; }
    public T15 Item15 { get; }

    protected override object? GetItem(int index)
        => index switch {
            0 => Item0,
            1 => Item1,
            2 => Item2,
            3 => Item3,
            4 => Item4,
            5 => Item5,
            6 => Item6,
            7 => Item7,
            8 => Item8,
            9 => Item9,
            10 => Item10,
            11 => Item11,
            12 => Item12,
            13 => Item13,
            14 => Item14,
            15 => Item15,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>? other)
    {
        if (other == null)
            return false;
        if (Item0 is not CancellationToken && !EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) return false;
        if (Item1 is not CancellationToken && !EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) return false;
        if (Item2 is not CancellationToken && !EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) return false;
        if (Item3 is not CancellationToken && !EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) return false;
        if (Item4 is not CancellationToken && !EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) return false;
        if (Item5 is not CancellationToken && !EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) return false;
        if (Item6 is not CancellationToken && !EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) return false;
        if (Item7 is not CancellationToken && !EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) return false;
        if (Item8 is not CancellationToken && !EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) return false;
        if (Item9 is not CancellationToken && !EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) return false;
        if (Item10 is not CancellationToken && !EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) return false;
        if (Item11 is not CancellationToken && !EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) return false;
        if (Item12 is not CancellationToken && !EqualityComparer<T12>.Default.Equals(Item12, other.Item12)) return false;
        if (Item13 is not CancellationToken && !EqualityComparer<T13>.Default.Equals(Item13, other.Item13)) return false;
        if (Item14 is not CancellationToken && !EqualityComparer<T14>.Default.Equals(Item14, other.Item14)) return false;
        if (Item15 is not CancellationToken && !EqualityComparer<T15>.Default.Equals(Item15, other.Item15)) return false;
        return true;
    }

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Item0 is CancellationToken || Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);
            hashCode = (hashCode * 397) + (Item1 is CancellationToken || Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));
            hashCode = (hashCode * 397) + (Item2 is CancellationToken || Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));
            hashCode = (hashCode * 397) + (Item3 is CancellationToken || Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));
            hashCode = (hashCode * 397) + (Item4 is CancellationToken || Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));
            hashCode = (hashCode * 397) + (Item5 is CancellationToken || Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));
            hashCode = (hashCode * 397) + (Item6 is CancellationToken || Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));
            hashCode = (hashCode * 397) + (Item7 is CancellationToken || Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));
            hashCode = (hashCode * 397) + (Item8 is CancellationToken || Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));
            hashCode = (hashCode * 397) + (Item9 is CancellationToken || Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));
            hashCode = (hashCode * 397) + (Item10 is CancellationToken || Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));
            hashCode = (hashCode * 397) + (Item11 is CancellationToken || Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));
            hashCode = (hashCode * 397) + (Item12 is CancellationToken || Item12 is null ? 0 : EqualityComparer<T12>.Default.GetHashCode(Item12));
            hashCode = (hashCode * 397) + (Item13 is CancellationToken || Item13 is null ? 0 : EqualityComparer<T13>.Default.GetHashCode(Item13));
            hashCode = (hashCode * 397) + (Item14 is CancellationToken || Item14 is null ? 0 : EqualityComparer<T14>.Default.GetHashCode(Item14));
            hashCode = (hashCode * 397) + (Item15 is CancellationToken || Item15 is null ? 0 : EqualityComparer<T15>.Default.GetHashCode(Item15));
            return hashCode;
        }
    }

    public bool Equals(ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>? other, Delegate?[] equalDelegates)
    {
        if (equalDelegates.Length != 16)
            throw new ArgumentOutOfRangeException(nameof(equalDelegates));
        if (other == null)
            return false;
        if (equalDelegates[0] is Func<T0, T0, bool> func0) {
            if (!func0.Invoke(Item0, other.Item0))
                return false;
        }
        else if (!EqualityComparer<T0>.Default.Equals(Item0, other.Item0)) {
            return false;
        }

        if (equalDelegates[1] is Func<T1, T1, bool> func1) {
            if (!func1.Invoke(Item1, other.Item1))
                return false;
        }
        else if (!EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) {
            return false;
        }

        if (equalDelegates[2] is Func<T2, T2, bool> func2) {
            if (!func2.Invoke(Item2, other.Item2))
                return false;
        }
        else if (!EqualityComparer<T2>.Default.Equals(Item2, other.Item2)) {
            return false;
        }

        if (equalDelegates[3] is Func<T3, T3, bool> func3) {
            if (!func3.Invoke(Item3, other.Item3))
                return false;
        }
        else if (!EqualityComparer<T3>.Default.Equals(Item3, other.Item3)) {
            return false;
        }

        if (equalDelegates[4] is Func<T4, T4, bool> func4) {
            if (!func4.Invoke(Item4, other.Item4))
                return false;
        }
        else if (!EqualityComparer<T4>.Default.Equals(Item4, other.Item4)) {
            return false;
        }

        if (equalDelegates[5] is Func<T5, T5, bool> func5) {
            if (!func5.Invoke(Item5, other.Item5))
                return false;
        }
        else if (!EqualityComparer<T5>.Default.Equals(Item5, other.Item5)) {
            return false;
        }

        if (equalDelegates[6] is Func<T6, T6, bool> func6) {
            if (!func6.Invoke(Item6, other.Item6))
                return false;
        }
        else if (!EqualityComparer<T6>.Default.Equals(Item6, other.Item6)) {
            return false;
        }

        if (equalDelegates[7] is Func<T7, T7, bool> func7) {
            if (!func7.Invoke(Item7, other.Item7))
                return false;
        }
        else if (!EqualityComparer<T7>.Default.Equals(Item7, other.Item7)) {
            return false;
        }

        if (equalDelegates[8] is Func<T8, T8, bool> func8) {
            if (!func8.Invoke(Item8, other.Item8))
                return false;
        }
        else if (!EqualityComparer<T8>.Default.Equals(Item8, other.Item8)) {
            return false;
        }

        if (equalDelegates[9] is Func<T9, T9, bool> func9) {
            if (!func9.Invoke(Item9, other.Item9))
                return false;
        }
        else if (!EqualityComparer<T9>.Default.Equals(Item9, other.Item9)) {
            return false;
        }

        if (equalDelegates[10] is Func<T10, T10, bool> func10) {
            if (!func10.Invoke(Item10, other.Item10))
                return false;
        }
        else if (!EqualityComparer<T10>.Default.Equals(Item10, other.Item10)) {
            return false;
        }

        if (equalDelegates[11] is Func<T11, T11, bool> func11) {
            if (!func11.Invoke(Item11, other.Item11))
                return false;
        }
        else if (!EqualityComparer<T11>.Default.Equals(Item11, other.Item11)) {
            return false;
        }

        if (equalDelegates[12] is Func<T12, T12, bool> func12) {
            if (!func12.Invoke(Item12, other.Item12))
                return false;
        }
        else if (!EqualityComparer<T12>.Default.Equals(Item12, other.Item12)) {
            return false;
        }

        if (equalDelegates[13] is Func<T13, T13, bool> func13) {
            if (!func13.Invoke(Item13, other.Item13))
                return false;
        }
        else if (!EqualityComparer<T13>.Default.Equals(Item13, other.Item13)) {
            return false;
        }

        if (equalDelegates[14] is Func<T14, T14, bool> func14) {
            if (!func14.Invoke(Item14, other.Item14))
                return false;
        }
        else if (!EqualityComparer<T14>.Default.Equals(Item14, other.Item14)) {
            return false;
        }

        if (equalDelegates[15] is Func<T15, T15, bool> func15) {
            if (!func15.Invoke(Item15, other.Item15))
                return false;
        }
        else if (!EqualityComparer<T15>.Default.Equals(Item15, other.Item15)) {
            return false;
        }

        return true;
    }

    public int GetHashCode(Delegate?[] getHashCodeDelegates)
    {
        if (getHashCodeDelegates.Length != 16)
            throw new ArgumentOutOfRangeException(nameof(getHashCodeDelegates));
        unchecked {
            int hashCode;
            if (getHashCodeDelegates[0] is Func<T0, int> func0)
                hashCode = func0(Item0);
            else
                hashCode = Item0 is null ? 0 : EqualityComparer<T0>.Default.GetHashCode(Item0);

            if (getHashCodeDelegates[1] is Func<T1, int> func1)
                hashCode = (hashCode * 397) + func1(Item1);
            else
                hashCode = (hashCode * 397) + (Item1 is null ? 0 : EqualityComparer<T1>.Default.GetHashCode(Item1));

            if (getHashCodeDelegates[2] is Func<T2, int> func2)
                hashCode = (hashCode * 397) + func2(Item2);
            else
                hashCode = (hashCode * 397) + (Item2 is null ? 0 : EqualityComparer<T2>.Default.GetHashCode(Item2));

            if (getHashCodeDelegates[3] is Func<T3, int> func3)
                hashCode = (hashCode * 397) + func3(Item3);
            else
                hashCode = (hashCode * 397) + (Item3 is null ? 0 : EqualityComparer<T3>.Default.GetHashCode(Item3));

            if (getHashCodeDelegates[4] is Func<T4, int> func4)
                hashCode = (hashCode * 397) + func4(Item4);
            else
                hashCode = (hashCode * 397) + (Item4 is null ? 0 : EqualityComparer<T4>.Default.GetHashCode(Item4));

            if (getHashCodeDelegates[5] is Func<T5, int> func5)
                hashCode = (hashCode * 397) + func5(Item5);
            else
                hashCode = (hashCode * 397) + (Item5 is null ? 0 : EqualityComparer<T5>.Default.GetHashCode(Item5));

            if (getHashCodeDelegates[6] is Func<T6, int> func6)
                hashCode = (hashCode * 397) + func6(Item6);
            else
                hashCode = (hashCode * 397) + (Item6 is null ? 0 : EqualityComparer<T6>.Default.GetHashCode(Item6));

            if (getHashCodeDelegates[7] is Func<T7, int> func7)
                hashCode = (hashCode * 397) + func7(Item7);
            else
                hashCode = (hashCode * 397) + (Item7 is null ? 0 : EqualityComparer<T7>.Default.GetHashCode(Item7));

            if (getHashCodeDelegates[8] is Func<T8, int> func8)
                hashCode = (hashCode * 397) + func8(Item8);
            else
                hashCode = (hashCode * 397) + (Item8 is null ? 0 : EqualityComparer<T8>.Default.GetHashCode(Item8));

            if (getHashCodeDelegates[9] is Func<T9, int> func9)
                hashCode = (hashCode * 397) + func9(Item9);
            else
                hashCode = (hashCode * 397) + (Item9 is null ? 0 : EqualityComparer<T9>.Default.GetHashCode(Item9));

            if (getHashCodeDelegates[10] is Func<T10, int> func10)
                hashCode = (hashCode * 397) + func10(Item10);
            else
                hashCode = (hashCode * 397) + (Item10 is null ? 0 : EqualityComparer<T10>.Default.GetHashCode(Item10));

            if (getHashCodeDelegates[11] is Func<T11, int> func11)
                hashCode = (hashCode * 397) + func11(Item11);
            else
                hashCode = (hashCode * 397) + (Item11 is null ? 0 : EqualityComparer<T11>.Default.GetHashCode(Item11));

            if (getHashCodeDelegates[12] is Func<T12, int> func12)
                hashCode = (hashCode * 397) + func12(Item12);
            else
                hashCode = (hashCode * 397) + (Item12 is null ? 0 : EqualityComparer<T12>.Default.GetHashCode(Item12));

            if (getHashCodeDelegates[13] is Func<T13, int> func13)
                hashCode = (hashCode * 397) + func13(Item13);
            else
                hashCode = (hashCode * 397) + (Item13 is null ? 0 : EqualityComparer<T13>.Default.GetHashCode(Item13));

            if (getHashCodeDelegates[14] is Func<T14, int> func14)
                hashCode = (hashCode * 397) + func14(Item14);
            else
                hashCode = (hashCode * 397) + (Item14 is null ? 0 : EqualityComparer<T14>.Default.GetHashCode(Item14));

            if (getHashCodeDelegates[15] is Func<T15, int> func15)
                hashCode = (hashCode * 397) + func15(Item15);
            else
                hashCode = (hashCode * 397) + (Item15 is null ? 0 : EqualityComparer<T15>.Default.GetHashCode(Item15));

            return hashCode;
        }
    }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
        Item11 = item11;
        Item12 = item12;
        Item13 = item13;
        Item14 = item14;
        Item15 = item15;
    }
}

