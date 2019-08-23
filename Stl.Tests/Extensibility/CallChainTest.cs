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
            void Add(int n, Invoker<int, int> chain)
            {
                chain.State += n;
                chain.Run();
            }
            Invoker.New(new int[0], 0, Add).Invoke().State.Should().Be(0);
            Invoker.New(new [] {10}, 1, Add).Invoke().State.Should().Be(11);
            Invoker.New(new [] {1, 2, 3}, 0, Add).Invoke().State.Should().Be(6);
        }
        
        [Fact]
        public void StatelessChainTest()
        {
            void Increment(Box<int> box, Invoker<Box<int>, Unit> chain)
            {
                box.Value += 1;
                chain.Run();
            }
            const int chainLength = 10; 
            var boxes = Enumerable.Range(0, chainLength).Select(i => Box.New(0)).ToArray();
            Invoker.New(boxes, Increment).Invoke();
            boxes.Sum(b => b.Value).Should().Be(chainLength);
        }
        
        [Fact]
        public async Task SimpleChainTestAsync()
        {
            async Task AddAsync(int n, AsyncInvoker<int, int> chain, CancellationToken ct)
            {
                chain.State += n;
                await chain.RunAsync(ct);
            }
            (await AsyncInvoker.New(new int[0], 0, AddAsync).InvokeAsync()).State
                .Should().Be(0);
            (await AsyncInvoker.New(new [] {10}, 1, AddAsync).InvokeAsync()).State
                .Should().Be(11);
            (await AsyncInvoker.New(new [] {1, 2, 3}, 0, AddAsync).InvokeAsync()).State
                .Should().Be(6);
        }
        
        [Fact]
        public async Task StatelessChainTestAsync()
        {
            async Task IncrementAsync(Box<int> box, AsyncInvoker<Box<int>, Unit> chain, CancellationToken ct)
            {
                box.Value += 1;
                await Task.Yield();
                await chain.RunAsync(ct).ConfigureAwait(false);
            }
            // It's important to have fairly long chain here:
            // the calls are async-recursive, so in this case
            // they shouldn't trigger StackOverflowException
            // even for very long chains
            const int chainLength = 100000; 
            var boxes = Enumerable.Range(0, chainLength).Select(i => Box.New(0)).ToArray();
            await AsyncInvoker.New(boxes, IncrementAsync).InvokeAsync();
            boxes.Sum(b => b.Value).Should().Be(chainLength);
        }
    }
}
