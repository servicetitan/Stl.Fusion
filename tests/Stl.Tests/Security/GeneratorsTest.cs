using Stl.Generators;

namespace Stl.Tests.Security;

public class GeneratorsTest : TestBase
{
    public GeneratorsTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void Int32GeneratorTest()
    {
        var g = new SequentialInt32Generator();
        g.Next().Should().Be(1);
        g.Next().Should().Be(2);
        g.Next().Should().Be(3);
    }

    [Fact]
    public void Int64GeneratorTest()
    {
        var g = new SequentialInt64Generator();
        g.Next().Should().Be(1);
        g.Next().Should().Be(2);
        g.Next().Should().Be(3);
    }

    [Fact]
    public void TransformingGeneratorTest()
    {
        var g = new TransformingGenerator<int, string>(new SequentialInt32Generator(), i => i.ToString());
        g.Next().Should().Be("1");
        g.Next().Should().Be("2");
        g.Next().Should().Be("3");
    }


    [Fact]
    public void RandomStringGeneratorTest()
    {
        var g = new RandomStringGenerator();
        ValidateGeneratedValues(
            Enumerable.Range(0, 1_000).Select(_ => g.Next()),
            16, RandomStringGenerator.DefaultAlphabet);

        g = new RandomStringGenerator(8, RandomStringGenerator.Base16Alphabet);
        ValidateGeneratedValues(
            Enumerable.Range(0, 1_000).Select(_ => g.Next()),
            8, RandomStringGenerator.Base16Alphabet);

        g = new RandomStringGenerator();
        ValidateGeneratedValues(
            Enumerable.Range(0, 1_000).Select(_ => g.Next(8, RandomStringGenerator.Base32Alphabet)),
            8, RandomStringGenerator.Base32Alphabet);
    }

    private static void ValidateGeneratedValues(IEnumerable<string> values, int expectedLength, string expectedAlphabet)
    {
        var l = values.ToList();
        var hs = new HashSet<string>(l);
        hs.Count.Should().Be(l.Count);
        foreach (var v in hs) {
            v.Length.Should().Be(expectedLength);
            v.All(expectedAlphabet.Contains).Should().BeTrue();
        }
    }
}
