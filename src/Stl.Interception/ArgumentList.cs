// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeConstructorOrDestructorBody
namespace Stl.Interception;

partial record ArgumentList
{
    public readonly ArgumentList Empty = new ();

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

public record ArgumentList<T0> : ArgumentList
{
    public T0 Item0 { get; }

    public ArgumentList(T0 item0)
    {
        Item0 = item0;
    }
}

public record ArgumentList<T0, T1> : ArgumentList
{
    public T0 Item0 { get; }
    public T1 Item1 { get; }

    public ArgumentList(T0 item0, T1 item1)
    {
        Item0 = item0;
        Item1 = item1;
    }
}

public record ArgumentList<T0, T1, T2> : ArgumentList
{
    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }

    public ArgumentList(T0 item0, T1 item1, T2 item2)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
    }
}

public record ArgumentList<T0, T1, T2, T3> : ArgumentList
{
    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
    }
}

public record ArgumentList<T0, T1, T2, T3, T4> : ArgumentList
{
    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }

    public ArgumentList(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4)
    {
        Item0 = item0;
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
    }
}

public record ArgumentList<T0, T1, T2, T3, T4, T5> : ArgumentList
{
    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }

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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6> : ArgumentList
{
    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }

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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7> : ArgumentList
{
    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }

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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8> : ArgumentList
{
    public T0 Item0 { get; }
    public T1 Item1 { get; }
    public T2 Item2 { get; }
    public T3 Item3 { get; }
    public T4 Item4 { get; }
    public T5 Item5 { get; }
    public T6 Item6 { get; }
    public T7 Item7 { get; }
    public T8 Item8 { get; }

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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : ArgumentList
{
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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ArgumentList
{
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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : ArgumentList
{
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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : ArgumentList
{
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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : ArgumentList
{
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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : ArgumentList
{
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

public record ArgumentList<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : ArgumentList
{
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

