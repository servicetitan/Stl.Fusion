using System.Threading.Channels;

namespace Stl.Channels
{
    public class ChannelPair<T>
    {
        public Channel<T> Channel1 { get; protected set; }
        public Channel<T> Channel2 { get; protected set; }

        protected ChannelPair() { }
        public ChannelPair(Channel<T> channel1, Channel<T> channel2)
        {
            Channel1 = channel1;
            Channel2 = channel2;
        }
    }

    public static class ChannelPair
    {
        public static ChannelPair<T> Create<T>(Channel<T> channel1, Channel<T> channel2) 
            => new ChannelPair<T>(channel1, channel2);

        public static ChannelPair<T> CreateTwisted<T>(Channel<T> channel1, Channel<T> channel2) 
            => new ChannelPair<T>(
                new CustomChannel<T>(channel1.Reader, channel2.Writer), 
                new CustomChannel<T>(channel2.Reader, channel1.Writer));
    }
}
