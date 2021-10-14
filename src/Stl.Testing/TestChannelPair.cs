using System.Threading.Channels;
using Stl.Channels;
using Xunit.Abstractions;

namespace Stl.Testing
{
    public class TestChannelPair<T> : ChannelPair<T>
    {
        public string Name { get; }
        public ITestOutputHelper? Out { get; }

        public TestChannelPair(string name, ITestOutputHelper? @out = null, int capacity = 16)
        {
            Name = name;
            Out = @out;
            var options = new BoundedChannelOptions(capacity) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = false,
                SingleWriter = false,
            };
            if (Out == null) {
                var cp = ChannelPair.CreateTwisted(
                    Channel.CreateBounded<T>(options),
                    Channel.CreateBounded<T>(options));
                Channel1 = cp.Channel1;
                Channel2 = cp.Channel2;
            }
            else {
                var cp1 = ChannelPair.CreateTwisted(
                    Channel.CreateBounded<T>(options),
                    Channel.CreateBounded<T>(options));
                var cp2 = ChannelPair.CreateTwisted(
                    Channel.CreateBounded<T>(options),
                    Channel.CreateBounded<T>(options));
                _ = cp1.Channel2.Connect(cp2.Channel1,
                    m => {
                        Out.WriteLine($"{Name}.Channel1 -> {m}");
                        return m;
                    },
                    m => {
                        Out.WriteLine($"{Name}.Channel2 -> {m}");
                        return m;
                    }
                );
                Channel1 = cp1.Channel1;
                Channel2 = cp2.Channel2;
            }
        }
    }
}
