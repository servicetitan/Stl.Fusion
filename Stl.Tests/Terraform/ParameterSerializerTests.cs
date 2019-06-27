using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Stl.ParametersSerializer;
using Stl.Terraform;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Terraform
{
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
            actual.Should().BeEquivalentTo("-stringParameter=stringValue");
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
                "-stringParameter=stringValue",
                "-enumParam=first",
                "-boolParam"
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
            actual.Should().BeEquivalentTo("-enumParam=first");
        }

        [Fact]
        public void TestEnumWithoutCliValue()
        {
            var actual = parameterSerializer.Serialize(new EnumParameter{Parameter = EnumParam.Second});
            actual.Should().BeEquivalentTo("-enumParam=Second");
        }

        [Fact]
        public void TestIntValue()
        {
            var actual = parameterSerializer.Serialize(new IntParameter{Parameter = 5});
            actual.Should().BeEquivalentTo("-intParam=5s");
        }
        
        [Fact]
        public void TestBoolTrue()
        {
            var actual = parameterSerializer.Serialize(new BoolParameter{Parameter = true});
            actual.Should().BeEquivalentTo("-boolParam");
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
            actual.Should().BeEquivalentTo("-var key1=value1 key2=value2");
        }

        private class StringParameter : IParameters
        {
            [CliParameter("-stringParameter={value}")]
            public string? Parameter{ get; set; }
        }        
        
        private class EnumParameter : IParameters
        {
            [CliParameter("-enumParam={value}")]
            public EnumParam? Parameter{ get; set; }
        }
        
        private class IntParameter : IParameters
        {
            [CliParameter("-intParam={value}s")]
            public int? Parameter{ get; set; }
        }
        
        private class BoolParameter : IParameters
        {
            [CliParameter("-boolParam")]
            public bool? Parameter{ get; set; }
        }
        
        private class DictionaryParameter : IParameters
        {
            [CliParameter("-var {value}", RepeatPattern = "{key}={value}", Separator = " ")]
            public IDictionary<string, string>? Parameter{ get; set; }
        }
        
        private class MultipleParameter : IParameters
        {
            [CliParameter("-stringParameter={value}")]
            public string? Parameter1{ get; set; }
            
            [CliParameter("-enumParam={value}")]
            public EnumParam? Parameter2{ get; set; }
            
            [CliParameter("-boolParam")]
            public bool? Parameter3{ get; set; }
        }
        
        private enum EnumParam
        {
            [CliValue("first")]
            First,
            Second,
        }
    }

    
}