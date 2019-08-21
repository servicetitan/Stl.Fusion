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
        public void DelegateChainTest()
        {
            var actions = new[] {
                (Action<ICallChain<int>>) (c => {
                    c.State.Should().Be(0);
                    c.State += 1;
                    c.State.Should().Be(1);
                    c.InvokeNext();
                }),
                c => {
                    c.State.Should().Be(1);
                    c.State += 2;
                    c.State.Should().Be(3);
                    c.InvokeNext();
                },
            };
            actions.ChainInvoke(0).Should().Be(3);
        }

        [Fact]
        public void SimpleChainTest()
        {
            void Add(int n, ICallChain<int> chain)
            {
                chain.State += n;
                chain.InvokeNext();
            }
            new int[] {}.ChainInvoke(0, Add).Should().Be(0);
            new [] {10}.ChainInvoke(1, Add).Should().Be(11);
            new[] {1, 2, 3}.ChainInvoke(0, Add).Should().Be(6);
        }
        
        [Fact]
        public void StatelessChainTest()
        {
            void Increment(Box<int> box, ICallChain<Unit> chain)
            {
                box.Value += 1;
                chain.InvokeNext();
            }
            const int chainLength = 10; 
            var boxes = Enumerable.Range(0, chainLength).Select(i => Box.New(0)).ToArray();
            boxes.ChainInvoke(Increment);
            boxes.Sum(b => b.Value).Should().Be(chainLength);
        }
        
        [Fact]
        public async Task DelegateChainTestAsync()
        {
            var actions = new[] {
                (Func<IAsyncCallChain<int>, CancellationToken, Task>) (async (c, ct) => {
                    c.State.Should().Be(0);
                    c.State += 1;
                    c.State.Should().Be(1);
                    await c.InvokeNextAsync(ct);
                }),
                async (c, ct) => {
                    c.State.Should().Be(1);
                    c.State += 2;
                    c.State.Should().Be(3);
                    await c.InvokeNextAsync(ct);
                },
            };
            (await actions.ChainInvokeAsync(0)).Should().Be(3);
        }

        [Fact]
        public async Task SimpleChainTestAsync()
        {
            async Task AddAsync(int n, IAsyncCallChain<int> chain, CancellationToken ct)
            {
                chain.State += n;
                await chain.InvokeNextAsync(ct);
            }
            (await new int[] {}.ChainInvokeAsync(0, AddAsync)).Should().Be(0);
            (await new [] {10}.ChainInvokeAsync(1, AddAsync)).Should().Be(11);
            (await new[] {1, 2, 3}.ChainInvokeAsync(0, AddAsync)).Should().Be(6);
        }
        
        
        [Fact]
        public async Task StatelessChainTestAsync()
        {
            async Task IncrementAsync(Box<int> box, IAsyncCallChain<Unit> chain, CancellationToken ct)
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
            await boxes.ChainInvokeAsync(IncrementAsync);
            boxes.Sum(b => b.Value).Should().Be(chainLength);
        }
        
    }
}
