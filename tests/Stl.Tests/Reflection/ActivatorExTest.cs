using System;
using FluentAssertions;
using MediatR;
using Stl.Reflection;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Reflection
{
    public class ActivatorExTest : TestBase
    {
        public class SimpleClass { }

        public ActivatorExTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void NewTest()
        {
            ActivatorEx.New<int>().Should().Be(0);
            ActivatorEx.New<int>(false).Should().Be(0);
            ActivatorEx.New<Unit>().Should().Be(default);
            ActivatorEx.New<Unit>(false).Should().Be(default);
            ActivatorEx.New<SimpleClass>().Should().BeOfType(typeof(SimpleClass));
            ActivatorEx.New<SimpleClass>(false).Should().BeOfType(typeof(SimpleClass));

            ((Func<string>) (() => ActivatorEx.New<string>()))
                .Should().Throw<InvalidOperationException>();
            ActivatorEx.New<string>(false).Should().Be(null);
        }
    }
}
