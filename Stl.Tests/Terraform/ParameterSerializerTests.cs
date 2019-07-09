using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Stl.CommandLine;
using Stl.Terraform;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Terraform
{
    // TODO: Fix this test.
    /*
    public class ParameterSerializerTests : TestBase
    {
        private readonly ParameterSerializer parameterSerializer;

        public ParameterSerializerTests(ITestOutputHelper @out) : base(@out)
        {
            parameterSerializer = new ParameterSerializer();
        }

        [Fact]
        public void TestString()
        {
            var actual = parameterSerializer.Serialize(new StringParameter{Parameter = "stringValue"});
            actual.Should().BeEquivalentTo((CliString)"-stringParameter=stringValue");
        }
        
        [Fact]
        public void TestMultiple()
        {
            var actual = parameterSerializer.Serialize(new MultipleParameter
            {
                Parameter1 = "stringValue",
                Parameter2 = EnumParam.First,
                Parameter3 = true,
            });
            actual.Should().BeEquivalentTo(
                (CliString)"-stringParameter=stringValue",
                (CliString)"-enumParam=first",
                (CliString)"-boolParam"
                );
        }
        
        [Fact]
        public void TestWhenNull()
        {
            var actual = parameterSerializer.Serialize(new StringParameter{Parameter = null});
            actual.Should().BeEmpty();
        }
        
        [Fact]
        public void TestEnumWithCliValue()
        {
            var actual = parameterSerializer.Serialize(new EnumParameter{Parameter = EnumParam.First});
            actual.Should().BeEquivalentTo((CliString)"-enumParam=first");
        }

        [Fact]
        public void TestEnumWithoutCliValue()
        {
            var actual = parameterSerializer.Serialize(new EnumParameter{Parameter = EnumParam.Second});
            actual.Should().BeEquivalentTo((CliString)"-enumParam=Second");
        }

        [Fact]
        public void TestIntValue()
        {
            var actual = parameterSerializer.Serialize(new IntParameter{Parameter = 5});
            actual.Should().BeEquivalentTo((CliString)"-intParam=5s");
        }
        
        [Fact]
        public void TestBoolTrue()
        {
            var actual = parameterSerializer.Serialize(new BoolParameter{Parameter = true});
            actual.Should().BeEquivalentTo((CliString)"-boolParam");
        }
        
        [Fact]
        public void TestBoolFalse()
        {
            var actual = parameterSerializer.Serialize(new BoolParameter{Parameter = false});
            actual.Should().BeEmpty();
        }
        
        [Fact]
        public void TestDictionaryValue()
        {
            var actual = parameterSerializer.Serialize(new DictionaryParameter{Parameter = 
                new Dictionary<string, string>
                {
                    {"key1", "value1"},
                    {"key2", "value2"},
                }
            });
            var cmdParts = new CliString[]{"-var key1=value1 key2=value2"};
            actual.Should().BeEquivalentTo(cmdParts);
        }

        private class StringParameter : IParameters
        {
            [CliArgument("-stringParameter={value}")]
            public string? Parameter{ get; set; }
        }        
        
        private class EnumParameter : IParameters
        {
            [CliArgument("-enumParam={value}")]
            public EnumParam? Parameter{ get; set; }
        }
        
        private class IntParameter : IParameters
        {
            [CliArgument("-intParam={value}s")]
            public int? Parameter{ get; set; }
        }
        
        private class BoolParameter : IParameters
        {
            [CliArgument("-boolParam")]
            public bool? Parameter{ get; set; }
        }
        
        private class DictionaryParameter : IParameters
        {
            [CliArgument("-var {value}", RepeatPattern = "{key}={value}", Separator = " ")]
            public IDictionary<string, string>? Parameter{ get; set; }
        }
        
        private class MultipleParameter : IParameters
        {
            [CliArgument("-stringParameter={value}")]
            public string? Parameter1{ get; set; }
            
            [CliArgument("-enumParam={value}")]
            public EnumParam? Parameter2{ get; set; }
            
            [CliArgument("-boolParam")]
            public bool? Parameter3{ get; set; }
        }
        
        private enum EnumParam
        {
            [CliValue("first")]
            First,
            Second,
        }
    }
    */
}
