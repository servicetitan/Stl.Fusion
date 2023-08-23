namespace Stl.Tests.Collections;

public class FenwickTreeTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void RandomTest()
    {
        var rnd = new Random(15);
        for (var size = 0; size < 10; size++) {
            var a = new int[size];
            for (var iteration = 0; iteration < 100; iteration ++) {
                for (var i = 0; i < size; i++)
                    a[i] = rnd.Next(10);
                var ft = new FenwickTree<int>(a, (x, y) => x + y);
                for (var i = 0; i <= size + 1; i++)
                    Assert.Equal(a.Take(i).Sum(), ft.GetSum(i - 1));
            }
        }
    }
}
