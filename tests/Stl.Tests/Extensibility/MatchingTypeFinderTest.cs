using System;
using FluentAssertions;
using Stl.Extensibility;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Extensibility
{
    public class MatchingTypeFinderTest : TestBase
    {
        public class NoMatch<T> { }
        public class G2<T1, T2> { }
        public class G1<T> : G2<int, T> { }
        public class Derived1 : G1<int> { }
        public class Derived2 : G2<int, string> { }
        public class Derived1D : Derived1 { }
        public class Derived2D : Derived2 { }

        [MatchFor(typeof(ValueType), typeof(MatchingTypeFinderTest))]
        public class MatchForValueType { }

        [MatchFor(typeof(int), typeof(MatchingTypeFinderTest))]
        public class MatchForInt { }

        [MatchFor(typeof(Lazy<>), typeof(MatchingTypeFinderTest))]
        public class MatchForLazy<T> { }

        [MatchFor(typeof(G2<,>), typeof(MatchingTypeFinderTest))]
        public class MatchForG2<T1, T2> { }
        [MatchFor(typeof(G1<>), typeof(MatchingTypeFinderTest))]
        public class MatchForG1<T> { }

        public MatchingTypeFinderTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicMatchTest()
        {
            var scope = GetType();
            var finder = new MatchingTypeFinder(scope.Assembly);
            finder.TryFind(typeof(object), null!).Should().BeNull();
            finder.TryFind(typeof(int), scope).Should().Be(typeof(MatchForInt));
            finder.TryFind(typeof(bool), scope).Should().Be(typeof(MatchForValueType));
            finder.TryFind(typeof(MatchingTypeFinderTest), scope).Should().BeNull();
            finder.TryFind(typeof(object), scope).Should().BeNull();
            ((Action) (() => finder.TryFind(null!, scope))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenericMatchTest()
        {
            var scope = GetType();
            var finder = new MatchingTypeFinder(scope.Assembly);

            finder.TryFind(typeof(NoMatch<int>), scope).Should().BeNull();

            finder.TryFind(typeof(Lazy<int>), scope).Should().Be(typeof(MatchForLazy<int>));
            finder.TryFind(typeof(Lazy<bool>), scope).Should().Be(typeof(MatchForLazy<bool>));

            finder.TryFind(typeof(Derived1), scope).Should().Be(typeof(MatchForG1<int>));
            finder.TryFind(typeof(Derived2), scope).Should().Be(typeof(MatchForG2<int, string>));
            finder.TryFind(typeof(Derived1D), scope).Should().Be(typeof(MatchForG1<int>));
            finder.TryFind(typeof(Derived2D), scope).Should().Be(typeof(MatchForG2<int, string>));
        }
    }
}
