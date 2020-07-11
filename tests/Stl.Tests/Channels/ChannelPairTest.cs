using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;
using Stl.Channels;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Channels
{
    public class ChannelPairTest : TestBase
    {
        public ChannelPairTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task TwistedPairTest()
        {
            var options = new BoundedChannelOptions(1) {
                AllowSynchronousContinuations = true,
            };
            var cp = ChannelPair.CreateTwisted(
                Channel.CreateBounded<int>(options),
                Channel.CreateBounded<int>(options));

            await PassThroughTest(cp.Channel1, cp.Channel2);
            await PassThroughTest(cp.Channel2, cp.Channel1);
        }

        [Fact]
        public async Task TestChannelPairTest()
        {
            var tp = new TestChannelPair<int>("Test", Out);
            await PassThroughTest(tp.Channel1, tp.Channel2);
            await PassThroughTest(tp.Channel2, tp.Channel1);
        }

        [Fact]
        public async Task ConnectTest1()
        {
            var options = new BoundedChannelOptions(1) {
                AllowSynchronousContinuations = true,
            };
            var cp1 = ChannelPair.CreateTwisted(
                Channel.CreateBounded<int>(options),
                Channel.CreateBounded<int>(options));
            var cp2 = ChannelPair.CreateTwisted(
                Channel.CreateBounded<int>(options),
                Channel.CreateBounded<int>(options));
            var _ = cp1.Channel2.ConnectAsync(cp2.Channel1, true);

            await PassThroughTest(cp1.Channel1, cp2.Channel2);
            await PassThroughTest(cp2.Channel2, cp1.Channel1);
        }

        [Fact]
        public async Task ConnectTest2()
        {
            var options = new BoundedChannelOptions(1) {
                AllowSynchronousContinuations = true,
            };
            var cp1 = ChannelPair.CreateTwisted(
                Channel.CreateBounded<int>(options),
                Channel.CreateBounded<int>(options));
            var cp2 = ChannelPair.CreateTwisted(
                Channel.CreateBounded<int>(options),
                Channel.CreateBounded<int>(options));
            var _ = cp1.Channel2.ConnectAsync(cp2.Channel1, true,
                m => {
                    Out.WriteLine($"-> {m}");
                    return m;
                },
                m => {
                    Out.WriteLine($"<- {m}");
                    return m;
                }
            );

            await PassThroughTest(cp1.Channel1, cp2.Channel2);
            await PassThroughTest(cp2.Channel2, cp1.Channel1);
        }

        private async Task PassThroughTest(Channel<int> c1, Channel<int> c2)
        {
            c2.Reader.Completion.IsCompleted.Should().BeFalse();

            var t1 = c1.Writer.AssertWriteAsync(1);
            var t2 = c2.Reader.AssertReadAsync();
            await t1;
            (await t2).Should().Be(1);

            t1 = c1.Writer.AssertWriteAsync(2);
            t2 = c2.Reader.AssertReadAsync();
            await t1;
            (await t2).Should().Be(2);

            c1.Writer.TryComplete().Should().BeTrue();
            await c2.Reader.AssertCompletedAsync();
        }
    }
}
