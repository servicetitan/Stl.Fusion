using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Extensibility;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Extensibility
{
    public class ServiceProviderExTest : TestBase
    {
        public class A
        {
            public string X { get; }
            public string Y { get; }

            public A(string x, string y)
            {
                X = x;
                Y = y;
            }

            public A(string x) : this(x, string.Empty) { }
        }

        public class B
        {
            public string X { get; }
            public string Y { get; }

            public B(string x, string y)
            {
                X = x;
                Y = y;
            }

            [ServiceConstructor]
            public B(string x) : this(x, string.Empty) { }
        }

        public class C
        {
            public string X { get; }
            public string Y { get; }

            public C(int x, int y)
            {
                X = x.ToString();
                Y = y.ToString();
            }
        }

        public ServiceProviderExTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void ActivateTest()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton("S");
            var services = new DefaultServiceProviderFactory()
                .CreateServiceProvider(serviceCollection);

            var a = (A) services.Activate(typeof(A));
            a.X.Should().Be("S");
            a.Y.Should().Be("S");

            var b = (B) services.Activate(typeof(B));
            b.X.Should().Be("S");
            b.Y.Should().BeNull();

            ((Action) (() => {
                var c = (C) services.Activate(typeof(C)); 
            })).Should().Throw<InvalidOperationException>();
        }
    }
}
