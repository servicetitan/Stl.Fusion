using System;
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
            [CliArgument("-stringArg={0}")]
            public string? String { get; set; }

            [CliArgument("-enumArg={0}")]
            public Gender? Enum { get; set; }
            [CliArgument("-enum2Arg={0:0}")]
            public Gender? Enum2 { get; set; }

            [CliArgument("-intArg={0}s", DefaultValue = "-1")]
            public int Int { get; set; } = -1;

            [CliArgument("-boolArg", DefaultValue="false")]
            public bool? Bool { get; set; }

            [CliArgument("-var {0}")]
            public CliDictionary<string, string>? Vars { get; set; }

            [CliArgument("-flag {0:Q}")]
            public CliList<string>? Flags { get; set; }

            [CliArgument(Priority = 1000)]
            public CliString Suffix { get; set; } = "";
        }

        protected class ArgumentsEx : Arguments
        {
            [CliArgument(IsRequired = true)]
            public string? Path { get; set; }

            [CliArgument(Priority = -1000)]
            public CliString Prefix { get; set; } = "";
        }

        protected class NestedArguments
        {
            [CliArgument]
            public ArgumentsEx Arguments { get; set; } = new ArgumentsEx();

            [CliArgument(IsRequired = true)]
            public string? Path { get; set; }
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
            actual.Value.Should().Be("-boolArg");

            actual = CliFormatter.Format(new Arguments {Bool = false});
            actual.Value.Should().BeEmpty();
        }

        [Fact]
        public void StringTest()
        {
            var actual = CliFormatter.Format(new Arguments {String = "v"});
            actual.Value.Should().Be("-stringArg=v");

            actual = CliFormatter.Format(new Arguments{ String = null});
            actual.Value.Should().BeEmpty();
        }

        [Fact]
        public void IntTest()
        {
            var actual = CliFormatter.Format(new Arguments {Int = 5});
            actual.Value.Should().Be("-intArg=5s");
        }

        [Fact]
        public void EnumTest()
        {
            var actual = CliFormatter.Format(new Arguments {Enum = Gender.Male});
            actual.Value.Should().Be("-enumArg=male");
            actual = CliFormatter.Format(new Arguments {Enum = Gender.Female});
            actual.Value.Should().Be("-enumArg=Female");

            actual = CliFormatter.Format(new Arguments {Enum2 = Gender.Male});
            actual.Value.Should().Be("-enum2Arg=0");
            actual = CliFormatter.Format(new Arguments {Enum2 = Gender.Female});
            actual.Value.Should().Be("-enum2Arg=1");
        }

        [Fact]
        public void ListTest()
        {
            var actual = CliFormatter.Format(new Arguments {
                Flags = new CliList<string> { "x", "yy" }
            });
            var q1 = CliString.Quote("x"); // Depends on OS, so can't use " or '
            var q2 = CliString.Quote("yy");
            actual.Value.Should().Be($"-flag {q1} -flag {q2}");
        }

        [Fact]
        public void DictionaryTest()
        {
            var actual = CliFormatter.Format(new Arguments {
                Vars = new CliDictionary<string, string> {
                    {"key1", "value1"},
                    {"key2", "value2"},
                }
            });
            actual.Value.Should().Be("-var key1=value1 -var key2=value2");
        }

        [Fact]
        public void RequiredTest()
        {
            Assert.Throws<ArgumentException>(() => {
                CliFormatter.Format(new ArgumentsEx());
            });
            var actual = CliFormatter.Format(new ArgumentsEx {
                Bool = true,
                Path = "."
            });
            actual.Value.Should().Be("-boolArg .");
        }

        [Fact]
        public void PriorityTest()
        {
            var actual = CliFormatter.Format(new ArgumentsEx {
                Bool = true,
                Path = ".",
                Prefix = "p",
                Suffix = "s"
            });
            actual.Value.Should().Be("p -boolArg . s");
        }

        [Fact]
        public void NestedTest()
        {
            var actual = CliFormatter.Format(new NestedArguments() {
                Arguments = new ArgumentsEx() {
                    Path = "p1",
                    Suffix = "s",
                    Prefix = "p"
                },
                Path = "p2",
            });
            actual.Value.Should().Be("p p1 s p2");
        }

        [Fact]
        public void CombinedTest()
        {
            var actual = CliFormatter.Format(new ArgumentsEx() {
                String = "stringValue",
                Enum = Gender.Male,
                Bool = true,
                Vars = new CliDictionary<string, string>() {{"k", "v"}},
                Path = "path",
                Prefix = "prefix",
                Suffix = "suffix"
            });
            actual.Value.Should().Be(
                "prefix " +
                "-stringArg=stringValue " +
                "-enumArg=male " +
                "-boolArg " +
                "-var k=v " +
                "path " +
                "suffix"
                );
        }
    }
}
