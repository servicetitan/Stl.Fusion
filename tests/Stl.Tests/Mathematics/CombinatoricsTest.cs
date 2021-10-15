using Stl.Mathematics;

namespace Stl.Tests.Mathematics;

public class CombinatoricsTest : TestBase
{
    public CombinatoricsTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void CnkTest()
    {
        Assert.Equal(1, Combinatorics.Cnk(0, 0));

        Assert.Equal(1, Combinatorics.Cnk(1, 0));
        Assert.Equal(1, Combinatorics.Cnk(1, 1));

        Assert.Equal(2, Combinatorics.Cnk(2, 1));
        Assert.Equal(1, Combinatorics.Cnk(2, 2));

        Assert.Equal(1, Combinatorics.Cnk(3, 0));
        Assert.Equal(3, Combinatorics.Cnk(3, 1));
        Assert.Equal(3, Combinatorics.Cnk(3, 2));
        Assert.Equal(1, Combinatorics.Cnk(3, 3));

        Assert.Equal(1, Combinatorics.Cnk(4, 0));
        Assert.Equal(4, Combinatorics.Cnk(4, 1));
        Assert.Equal(6, Combinatorics.Cnk(4, 2));
        Assert.Equal(4, Combinatorics.Cnk(4, 3));
        Assert.Equal(1, Combinatorics.Cnk(4, 4));
    }

    [Fact]
    public void SubsetsTest()
    {
        var source = new [] {"A", "B", "C", "D"}.AsMemory();
        foreach (var subset in Combinatorics.Subsets(source, true)) {
            Out.WriteLine(subset.ToArray().ToDelimitedString());
        }
    }

    [Fact]
    public void KOfNTest()
    {
        void AllKOfN(int n, int k, bool exactlyK)
        {
            Out.WriteLine($"{k} of {n} {(exactlyK ? "(exactly)" : "")}:");
            var allKofN = Combinatorics.KOfN(n, k, exactlyK);
            foreach (var subset in allKofN)
                Out.WriteLine("  " + subset.ToDelimitedString());
            if (exactlyK) {
                var expected = (int) Combinatorics.Cnk(n, k);
                Assert.Equal(expected, allKofN.Count());
            }
        }

        AllKOfN(5, 5, false);
        AllKOfN(5, 4, true);
        AllKOfN(5, 4, false);
        AllKOfN(5, 3, true);
        AllKOfN(5, 3, false);
        AllKOfN(5, 2, true);
        AllKOfN(5, 2, false);
    }
}
