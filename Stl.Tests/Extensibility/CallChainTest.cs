using System;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Extensibility;
using Stl.Internal;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Extensibility
{
    public class CallChainTest : TestBase
    {
        public CallChainTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void SimpleChainTest()
        {
            void Add(int n, ChainInvocation<int, int> chain)
            {
                chain.State += n;
                chain.InvokeNext();
            }
            ChainInvocation.New(new int[0], 0, Add).Invoke().State.Should().Be(0);
            ChainInvocation.New(new [] {10}, 1, Add).Invoke().State.Should().Be(11);
            ChainInvocation.New(new [] {1, 2, 3}, 0, Add).Invoke().State.Should().Be(6);
        }
        
        [Fact]
        public void StatelessChainTest()
        {
            void Increment(Box<int> box, ChainInvocation<Box<int>, Unit> chain)
            {
                box.Value += 1;
                chain.InvokeNext();
            }
            const int chainLength = 10; 
            var boxes = Enumerable.Range(0, chainLength).Select(i => Box.New(0)).ToArray();
            ChainInvocation.New(boxes, Increment).Invoke();
            boxes.Sum(b => b.Value).Should().Be(chainLength);
        }
        
        [Fact]
        public async Task SimpleChainTestAsync()
        {
            async Task AddAsync(int n, AsyncChainInvocation<int, int> chain, CancellationToken ct)
            {
                chain.State += n;
                await chain.InvokeNextAsync(ct);
            }
            (await AsyncChainInvocation.New(new int[0], 0, AddAsync).InvokeAsync()).State
                .Should().Be(0);
            (await AsyncChainInvocation.New(new [] {10}, 1, AddAsync).InvokeAsync()).State
                .Should().Be(11);
            (await AsyncChainInvocation.New(new [] {1, 2, 3}, 0, AddAsync).InvokeAsync()).State
                .Should().Be(6);
        }
        
        [Fact]
        public async Task StatelessChainTestAsync()
        {
            async Task IncrementAsync(Box<int> box, AsyncChainInvocation<Box<int>, Unit> chain, CancellationToken ct)
            {
                box.Value += 1;
                await Task.Yield();
                await chain.InvokeNextAsync(ct).ConfigureAwait(false);
            }
            // It's important to have fairly long chain here:
            // the calls are async-recursive, so in this case
            // they shouldn't trigger StackOverflowException
            // even for very long chains
            const int chainLength = 100000; 
            var boxes = Enumerable.Range(0, chainLength).Select(i => Box.New(0)).ToArray();
            await AsyncChainInvocation.New(boxes, IncrementAsync).InvokeAsync();
            boxes.Sum(b => b.Value).Should().Be(chainLength);
        }
    }
}
