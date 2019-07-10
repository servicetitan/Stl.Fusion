using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Stl.CommandLine;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandLine
{
    public class CliFormatterTest : TestBase
    {
        protected class Arguments
        {
            [CliArgument("-stringParameter={0}")]
            public string? String { get; set; }

            [CliArgument("-enumParam={0}")]
            public Gender? Enum { get; set; }
            [CliArgument("-enum2Param={0:0}")]
            public Gender? Enum2 { get; set; }

            [CliArgument("-intParam={0}s", DefaultValue = "-1")]
            public int Int { get; set; } = -1;

            [CliArgument("-boolParam", DefaultValue="false")]
            public bool? Bool { get; set; }

            [CliArgument("-var {0}")]
            public CliDictionary<string, string>? Vars { get; set; }
        }
        
        protected enum Gender
        {
            [Display(Name = "male")]
            Male,
            Female,
        }

        protected CliFormatter CliFormatter { get; } = new CliFormatter();

        public CliFormatterTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void DefaultsTest()
        {
            var actual = CliFormatter.Format(new Arguments());
            actual.Value.Should().BeEmpty();
        }

        [Fact]
        public void BoolTest()
        {
            var actual = CliFormatter.Format(new Arguments {Bool = true});
            actual.Value.Should().Be("-boolParam");

            actual = CliFormatter.Format(new Arguments {Bool = false});
            actual.Value.Should().BeEmpty();
        }
        
        [Fact]
        public void StringTest()
        {
            var actual = CliFormatter.Format(new Arguments {String = "v"});
            actual.Value.Should().Be("-stringParameter=v");

            actual = CliFormatter.Format(new Arguments{ String = null});
            actual.Value.Should().BeEmpty();
        }

        [Fact]
        public void IntTest()
        {
            var actual = CliFormatter.Format(new Arguments {Int = 5});
            actual.Value.Should().Be("-intParam=5s");
        }
        
        [Fact]
        public void EnumTest()
        {
            var actual = CliFormatter.Format(new Arguments {Enum = Gender.Male});
            actual.Value.Should().Be("-enumParam=male");
            actual = CliFormatter.Format(new Arguments {Enum = Gender.Female});
            actual.Value.Should().Be("-enumParam=Female");

            actual = CliFormatter.Format(new Arguments {Enum2 = Gender.Male});
            actual.Value.Should().Be("-enum2Param=0");
            actual = CliFormatter.Format(new Arguments {Enum2 = Gender.Female});
            actual.Value.Should().Be("-enum2Param=1");
        }

        [Fact]
        public void MultipleTest()
        {
            var actual = CliFormatter.Format(new Arguments {
                String = "stringValue",
                Enum = Gender.Male,
                Bool = true,
            });
            actual.Value.Should().Be(
                "-stringParameter=stringValue " +
                "-enumParam=male " +
                "-boolParam"
                );
        }

        [Fact]
        public void CliDictionaryTest()
        {
            var actual = CliFormatter.Format(new Arguments {
                Vars = new CliDictionary<string, string> {
                    {"key1", "value1"},
                    {"key2", "value2"},
                }
            });
            actual.Value.Should().Be("-var key1=value1 -var key2=value2");
        }
    }
}
