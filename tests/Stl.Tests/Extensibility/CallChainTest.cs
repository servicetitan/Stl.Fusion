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
            void Add(int n, Invoker<int, int> invoker)
            {
                invoker.State += n;
                invoker.Invoke();
            }
            Invoker.New(new int[0], 0, Add).Run().State.Should().Be(0);
            Invoker.New(new [] {10}, 1, Add).Run().State.Should().Be(11);
            Invoker.New(new [] {1, 2, 3}, 0, Add).Run().State.Should().Be(6);
        }

        [Fact]
        public void OrderAndErrorHandlingTest()
        {
            void Concat1(string s, Invoker<string, string> invoker)
            {
                if (s == "e")
                    throw new ArgumentException();
                invoker.State += s;
                invoker.Invoke();
            }

            void Concat2(string s, Invoker<string, string> invoker)
            {
                if (s == "e")
                    throw new ArgumentException();
                invoker.State += s;
            }

            Invoker.New(new [] {"a", "b"}, "", Concat1)
                .Run().State.Should().Be("ab");
            Invoker.New(new [] {"a", "b"}, "", Concat1, InvocationOrder.Reverse)
                .Run().State.Should().Be("ba");
            Invoker.New(new [] {"a", "b"}, "", Concat2)
                .Run().State.Should().Be("ab");
            Invoker.New(new [] {"a", "b"}, "", Concat2, InvocationOrder.Reverse)
                .Run().State.Should().Be("ba");

            Action action;

            action = () => Invoker.New(new [] {"a", "e"}, "", Concat1).Run();
            action.Should().Throw<ArgumentException>();
            action = () => Invoker.New(new [] {"a", "e"}, "", Concat1, InvocationOrder.Reverse).Run();
            action.Should().Throw<ArgumentException>();
            action = () => Invoker.New(new [] {"a", "e"}, "", Concat2).Run();
            action.Should().Throw<ArgumentException>();
            action = () => Invoker.New(new [] {"a", "e"}, "", Concat2, InvocationOrder.Reverse).Run();
            action.Should().Throw<ArgumentException>();

            var errorHandlerCalled = false;
            void ErrorHandler(Exception e, string s, Invoker<string, string> invoker)
                => errorHandlerCalled = true;

            action = () => {
                errorHandlerCalled = false;
                Invoker.New(new[] {"a", "e"}, "", Concat1, InvocationOrder.Straight, ErrorHandler).Run();
                errorHandlerCalled.Should().BeTrue();
            };
            action.Should().NotThrow();
            action = () => {
                errorHandlerCalled = false;
                Invoker.New(new[] {"a", "e"}, "", Concat1, InvocationOrder.Reverse, ErrorHandler).Run();
                errorHandlerCalled.Should().BeTrue();
            };
            action.Should().NotThrow();
        }

        [Fact]
        public void StatelessChainTest()
        {
            void Increment(Box<int> box, Invoker<Box<int>, Unit> chain)
            {
                box.Value += 1;
                chain.Invoke();
            }
            const int chainLength = 10;
            var boxes = Enumerable.Range(0, chainLength).Select(i => Box.New(0)).ToArray();
            Invoker.New(boxes, Increment).Run();
            boxes.Sum(b => b.Value).Should().Be(chainLength);
        }

        [Fact]
        public async Task SimpleChainTestAsync()
        {
            async Task AddAsync(int n, AsyncInvoker<int, int> chain, CancellationToken ct)
            {
                chain.State += n;
                await chain.InvokeAsync(ct);
            }
            (await AsyncInvoker.New(new int[0], 0, AddAsync).RunAsync()).State
                .Should().Be(0);
            (await AsyncInvoker.New(new [] {10}, 1, AddAsync).RunAsync()).State
                .Should().Be(11);
            (await AsyncInvoker.New(new [] {1, 2, 3}, 0, AddAsync).RunAsync()).State
                .Should().Be(6);
        }


        [Fact]
        public async Task OrderAndErrorHandlingTestAsync()
        {
            async Task Concat1Async(string s, AsyncInvoker<string, string> invoker, CancellationToken cancellationToken)
            {
                if (s == "e")
                    throw new ArgumentException();
                invoker.State += s;
                await invoker.InvokeAsync(cancellationToken);
            }

            async Task Concat2Async(string s, AsyncInvoker<string, string> invoker, CancellationToken cancellationToken)
            {
                await Task.Yield();
                if (s == "e")
                    throw new ArgumentException();
                invoker.State += s;
            }

            (await AsyncInvoker.New(new [] {"a", "b"}, "", Concat1Async).RunAsync())
                .State.Should().Be("ab");
            (await AsyncInvoker.New(new [] {"a", "b"}, "", Concat1Async, InvocationOrder.Reverse).RunAsync())
                .State.Should().Be("ba");
            (await AsyncInvoker.New(new [] {"a", "b"}, "", Concat2Async).RunAsync())
                .State.Should().Be("ab");
            (await AsyncInvoker.New(new [] {"a", "b"}, "", Concat2Async, InvocationOrder.Reverse).RunAsync())
                .State.Should().Be("ba");

            Action action;

            action = () => AsyncInvoker.New(new [] {"a", "e"}, "", Concat1Async)
                .RunAsync().Result.Ignore();
            action.Should().Throw<ArgumentException>();
            action = () => AsyncInvoker.New(new [] {"a", "e"}, "", Concat1Async, InvocationOrder.Reverse)
                .RunAsync().Result.Ignore();
            action.Should().Throw<ArgumentException>();
            action = () => AsyncInvoker.New(new [] {"a", "e"}, "", Concat2Async)
                .RunAsync().Result.Ignore();
            action.Should().Throw<ArgumentException>();
            action = () => AsyncInvoker.New(new [] {"a", "e"}, "", Concat2Async, InvocationOrder.Reverse)
                .RunAsync().Result.Ignore();
            action.Should().Throw<ArgumentException>();

            var errorHandlerCalled = false;
            void ErrorHandler(Exception e, string s, AsyncInvoker<string, string> invoker)
                => errorHandlerCalled = true;

            action = () => {
                errorHandlerCalled = false;
                AsyncInvoker.New(new[] {"a", "e"}, "", Concat1Async, InvocationOrder.Straight, ErrorHandler)
                    .RunAsync().Result.Ignore();
                errorHandlerCalled.Should().BeTrue();
            };
            action.Should().NotThrow();
            action = () => {
                errorHandlerCalled = false;
                AsyncInvoker.New(new[] {"a", "e"}, "", Concat1Async, InvocationOrder.Reverse, ErrorHandler)
                    .RunAsync().Result.Ignore();
                errorHandlerCalled.Should().BeTrue();
            };
            action.Should().NotThrow();
        }

        [Fact]
        public async Task StatelessChainTestAsync()
        {
            async Task IncrementAsync(Box<int> box, AsyncInvoker<Box<int>, Unit> chain, CancellationToken ct)
            {
                box.Value += 1;
                await Task.Yield();
                await chain.InvokeAsync(ct).ConfigureAwait(false);
            }
            // It's important to have fairly long chain here:
            // the calls are async-recursive, so in this case
            // they shouldn't trigger StackOverflowException
            // even for very long chains
            const int chainLength = 1000;
            var boxes = Enumerable.Range(0, chainLength).Select(i => Box.New(0)).ToArray();
            await AsyncInvoker.New(boxes, IncrementAsync).RunAsync();
            boxes.Sum(b => b.Value).Should().Be(chainLength);
        }
    }
}
