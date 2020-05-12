using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Channels;
using Xunit.Abstractions;

namespace Stl.Testing
{
    public class TestChannelPair<T>
    {
        public string Name { get; }
        public Channel<T> ConsumerChannel { get; } 
        public Channel<T> TestChannel { get; }

        public TestChannelPair(string name, int capacity = 16, ITestOutputHelper? copyOutputTo = null)
        {
            Name = name;
            var cp = ChannelPair.CreateTwisted(
                Channel.CreateBounded<T>(capacity), 
                Channel.CreateBounded<T>(capacity));
            ConsumerChannel = cp.Channel1;
            TestChannel = cp.Channel2;

            if (copyOutputTo != null) {
                var tp = ChannelPair.CreateTwisted(
                    Channel.CreateBounded<T>(capacity), 
                    Channel.CreateBounded<T>(capacity));
                var middleChannel = TestChannel;
                var endChannel = tp.Channel1;
                TestChannel = tp.Channel2;

                Task.Run(() => middleChannel.Reader.TransformAsync(endChannel, true,
                    m => {
                        copyOutputTo.WriteLine($"{Name}: {m}");
                        return m;
                    }));
                Task.Run(() => endChannel.Reader.CopyAsync(middleChannel, true));
            }
        }
    }
}
