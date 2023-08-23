using Stl.Diagnostics;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Tests.Internal;

public class InvalidatedHandlerSetTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void Test()
    {
        const int iterationCount = 200;
        for (var iteration = 0; iteration < iterationCount; iteration++)
            for (var size = 0; size < 10; size++)
                RunTest(size, (iteration + 1.0) / iterationCount);
    }

    private void RunTest(int size, double removalProbability)
    {
        var usedIndexes = new HashSet<int>();

        var indexes = Enumerable.Range(0, size).ToList();
        var actions = indexes.Select(CreateAction).ToList();
        var handlerSet = new InvalidatedHandlerSet(actions);

        usedIndexes.Clear();
        handlerSet.Invoke(null!);
        usedIndexes.Count.Should().Be(actions.Count);

        var sampler = Sampler.RandomShared(removalProbability);
        var removedIndexes = new HashSet<int>(
            indexes.Where(_ => sampler.Next()));
        var removedActions = new HashSet<Action<IComputed>>(
            actions.Where((_, i) => removedIndexes.Contains(i)));
        actions = actions.Where(a => !removedActions.Contains(a)).ToList();
        foreach (var action in removedActions)
            handlerSet.Remove(action);

        usedIndexes.Clear();
        handlerSet.Invoke(null!);
        usedIndexes.Count.Should().Be(actions.Count);
        if (usedIndexes.Count != 0)
            usedIndexes.Should().AllSatisfy(i => removedIndexes.Contains(i).Should().BeFalse());

        Action<IComputed> CreateAction(int index)
            => _ => usedIndexes.Add(index).Should().BeTrue();
    }
}
