using Stl.Conversion;

namespace Stl.Tests.Conversion;

public class ConverterProviderTest(ITestOutputHelper @out) : TestBase(@out)
{
    public record LikeBool(bool Value) : IConvertibleTo<bool>
    {
        public static readonly LikeBool False = new(false);
        public static readonly LikeBool True = new(true);

        public LikeBool() : this(false) { }
        public override string ToString() => Value.ToString();

        public bool Convert() => Value;
        public static bool TryParse(string source, out LikeBool result)
        {
            result = False;
            var isParsed = bool.TryParse(source, out var v);
            if (isParsed)
                result = new LikeBool(v);
            return isParsed;
        }
    }

    public record LikeBool2(bool Value) : IConvertibleTo<bool>
    {
        public static readonly LikeBool2 False = new(false);
        public static readonly LikeBool2 True = new(true);

        public LikeBool2() : this(false) { }
        public override string ToString() => Value.ToString();

        public bool Convert() => Value;
        public static LikeBool2 Parse(string source)
            => new(bool.Parse(source));
    }

    public virtual IConverterProvider GetConverters()
    {
        var services = new ServiceCollection();
        services.AddConverters();
        services.AddSingleton<Func<int, Option<bool>>>(
            s => s == 0 ? false : s == 1 ? true : Option<bool>.None);
        services.AddSingleton<Func<long, int>>(s => checked ((int) s));
        return services.BuildServiceProvider().Converters();
    }

    [Fact]
    public void DefaultConvertersTest()
    {
        var c = new ServiceCollection().BuildServiceProvider().Converters();
        var c1 = c.From<string>().To<bool>().ThrowIfUnavailable();
        c1.Convert("true").Should().BeTrue();
    }

    [Fact]
    public void BoolTest()
    {
        var c = GetConverters();
        var c1 = c.From<string>().To<bool>().ThrowIfUnavailable();
        c1.Convert("true").Should().BeTrue();
        c1.Convert("false").Should().BeFalse();

        c1.TryConvert("true").Should().Be(Option.Some(true));
        c1.TryConvert("false").Should().Be(Option.Some(false));
        c1.TryConvert("_").Should().Be(Option.None<bool>());

        c1.TryConvertUntyped("true").Should().Be(Option.Some<object>(true));
        c1.TryConvertUntyped("_").Should().Be(Option.None<object>());
        Assert.Throws<InvalidOperationException>(() => c1.Convert("_"));
    }

    [Fact]
    public void LikeBoolTest()
    {
        var c = GetConverters();
        var c1 = c.From<string>().To<LikeBool>().ThrowIfUnavailable();
        c1.Convert("true").Value.Should().BeTrue();
        c1.Convert("false").Value.Should().BeFalse();
        c1.TryConvert("_").IsNone().Should().BeTrue();
        Assert.Throws<InvalidOperationException>(() => c1.Convert("_"));

        var c2 = c.From<LikeBool>().To<bool>().ThrowIfUnavailable();
        c2.Convert(LikeBool.False).Should().BeFalse();
        c2.Convert(LikeBool.True).Should().BeTrue();
    }

    [Fact]
    public void LikeBool2Test()
    {
        var c = GetConverters();
        var c1 = c.From<string>().To<LikeBool2>().ThrowIfUnavailable();
        c1.Convert("true").Value.Should().BeTrue();
        c1.Convert("false").Value.Should().BeFalse();
        c1.TryConvert("_").IsNone().Should().BeTrue();
        Assert.Throws<FormatException>(() => c1.Convert("_"));

        var c2 = c.From<LikeBool2>().To<bool>().ThrowIfUnavailable();
        c2.Convert(LikeBool2.False).Should().BeFalse();
        c2.Convert(LikeBool2.True).Should().BeTrue();
    }

    [Fact]
    public void FuncOptionTest()
    {
        var c = GetConverters();
        var c1 = c.From<int>().To<bool>().ThrowIfUnavailable();
        c1.Convert(1).Should().BeTrue();
        c1.Convert(0).Should().BeFalse();
        c1.TryConvert(10).IsNone().Should().BeTrue();
        Assert.Throws<InvalidOperationException>(() => c1.Convert(10));
    }

    [Fact]
    public void FuncTest()
    {
        var c = GetConverters();
        var c1 = c.From<long>().To<int>().ThrowIfUnavailable();
        c1.Convert(1).Should().Be(1);
        c1.Convert(0).Should().Be(0);
        c1.TryConvert(long.MaxValue).IsNone().Should().BeTrue();
        Assert.Throws<OverflowException>(() => c1.Convert(long.MaxValue));
    }

    [Fact]
    public void ConvertibleToTest()
    {
        var c = GetConverters();
        var c1 = c.From<Result<int>>().To<int>().ThrowIfUnavailable();
        c1.Convert(1).Should().Be(1);
        c1.Convert(0).Should().Be(0);

        var rb1 = ResultBox.New(1);
        var rb2 = ResultBox.New(2);
        var c2 = c.From(rb1.GetType()).To<Result<int>>();
        ((Result<int>) c1.Convert(rb1)).Should().Be(rb1.AsResult());
        ((Result<int>) c1.Convert(rb2)).Should().Be(rb2.AsResult());
    }

    [Fact]
    public void CastTest()
    {
        var c = GetConverters();
        var cio = c.From<int>().To<object>().ThrowIfUnavailable();
        var coi = c.From<object>().To<int>().ThrowIfUnavailable();
        var cii = c.From<int>().To<int>().ThrowIfUnavailable();
        var coo = c.From<object>().To<object>().ThrowIfUnavailable();
        cio.Convert(1).Should().Be(1);
        coi.Convert(2).Should().Be(2);
        cii.Convert(3).Should().Be(3);
        coo.Convert(4).Should().Be(4);

        var cso = c.From<string>().To<object>().ThrowIfUnavailable();
        var cos = c.From<object>().To<string>().ThrowIfUnavailable();
        var css = c.From<string>().To<string>().ThrowIfUnavailable();
        var s = "1";
        cso.Convert(s).Should().BeSameAs(s);
        cos.Convert(s).Should().BeSameAs(s);
        css.Convert(s).Should().BeSameAs(s);
        coo.Convert(s).Should().BeSameAs(s);
    }

    [Fact]
    public void NoConverterTest()
    {
        var c = GetConverters();

        var c1 = c.From<Guid>().To<int>();
        c1.IsAvailable.Should().BeFalse();
        Assert.Throws<NotSupportedException>(() => c1.ThrowIfUnavailable());
        Assert.Throws<NotSupportedException>(() => c1.Convert(Guid.Empty));
        Assert.Throws<NotSupportedException>(() => c1.ConvertUntyped(Guid.Empty));
        Assert.Throws<NotSupportedException>(() => c1.TryConvert(Guid.Empty));
        Assert.Throws<NotSupportedException>(() => c1.TryConvertUntyped(Guid.Empty));

        Assert.Throws<NotSupportedException>(
            () => c.From<Guid>().To(typeof(int)).ThrowIfUnavailable());
        Assert.Throws<NotSupportedException>(
            () => c.From(typeof(Guid)).To(typeof(int)).ThrowIfUnavailable());
    }
}
