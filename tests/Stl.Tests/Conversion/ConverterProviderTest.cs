using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Conversion;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Conversion
{
    public class ConverterProviderTest : TestBase
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

        public ConverterProviderTest(ITestOutputHelper @out) : base(@out) { }

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
            var converters = new ServiceCollection().BuildServiceProvider().Converters();

            var c1 = converters.From<string>().To<bool>().ThrowIfUnavailable();
            c1.Convert("true").Should().BeTrue();

            var c2 = converters.From<string>().To<LikeBool>().ThrowIfUnavailable();
            c2.Convert("true").Value.Should().BeTrue();
            c2.Convert("false").Value.Should().BeFalse();
        }
    }
}
