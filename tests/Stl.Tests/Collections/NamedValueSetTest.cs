using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Collections;
using Stl.Testing;
using Xunit;

namespace Stl.Tests.Collections
{
    public class NamedValueSetTest
    {
        [Fact]
        public void BasicTest()
        {
            var nvs = new NamedValueSet();
            nvs = nvs.PassThroughAllSerializers();
            nvs.Items.Count.Should().Be(0);

            nvs.Set("A");
            nvs = nvs.PassThroughAllSerializers();
            nvs.Get<string>().Should().Be("A");
            nvs.GetRequiredService<string>().Should().Be("A");
            nvs.Items.Count.Should().Be(1);

            nvs.Set("B");
            nvs = nvs.PassThroughAllSerializers();
            nvs.Get<string>().Should().Be("B");
            nvs.GetRequiredService<string>().Should().Be("B");
            nvs.Items.Count.Should().Be(1);

            nvs.Remove<string>();
            nvs = nvs.PassThroughAllSerializers();
            nvs.TryGet<string>().Should().Be(Option<string>.None);
            Assert.Throws<KeyNotFoundException>(() => {
                nvs.Get<string>();
            });
            nvs.GetService<string>().Should().Be(null);
            nvs.Items.Count.Should().Be(0);
        }
    }
}
