using System.Threading;
using Stl.Text;

namespace Stl.Fusion.Publication
{
    public interface IClientKeyProvider
    {
        Symbol NewClientKey();
        void ValidateClientId(Symbol clientKey);
    }

    public class SimpleClientKeyProvider : IClientKeyProvider
    {
        private long _clientKey;

        public Symbol NewClientKey() 
            => Interlocked.Increment(ref _clientKey).ToString();

        public void ValidateClientId(Symbol clientKey) 
        { }
    }
}
