using System;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Extensibility;
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
            new int[] {}.ChainInvoke(Add, 0).Should().Be(0);
            new [] {10}.ChainInvoke(Add, 1).Should().Be(11);
            new[] {1, 2, 3}.ChainInvoke(Add, 0).Should().Be(6);
        }
        
        [Fact]
        public async Task DelegateChainTestAsync()
        {
            var actions = new[] {
                (Func<IAsyncCallChain<int>, Task>) (async c => {
                    c.State.Should().Be(0);
                    c.State += 1;
                    c.State.Should().Be(1);
                    await c.InvokeNextAsync();
                }),
                async c => {
                    c.State.Should().Be(1);
                    c.State += 2;
                    c.State.Should().Be(3);
                    await c.InvokeNextAsync();
                },
            };
            (await actions.ChainInvokeAsync(0)).Should().Be(3);
        }

        [Fact]
        public async Task SimpleChainTestAsync()
        {
            async Task AddAsync(int n, IAsyncCallChain<int> chain)
            {
                chain.State += n;
                await chain.InvokeNextAsync();
            }
            (await new int[] {}.ChainInvokeAsync(AddAsync, 0)).Should().Be(0);
            (await new [] {10}.ChainInvokeAsync(AddAsync, 1)).Should().Be(11);
            (await new[] {1, 2, 3}.ChainInvokeAsync(AddAsync, 0)).Should().Be(6);
        }
    }
}