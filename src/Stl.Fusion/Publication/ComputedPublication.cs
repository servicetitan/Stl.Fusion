using System;
using Stl.Text;

namespace Stl.Fusion.Publication
{
    public interface IComputedPublication : IDisposable
    {
        IComputedPublisher Publisher { get; }
        Symbol Key { get; }
        Symbol ClientKey { get; }
        IComputed Computed { get; }
    }

    public abstract class ComputedPublicationBase : IComputedPublication
    {
        public IComputedPublisher Publisher { get; }
        public Symbol Key { get; }
        public Symbol ClientKey { get; }
        public IComputed Computed { get; }
        public IRenewalPolicy RenewalPolicy { get; }

        protected ComputedPublicationBase(IComputedPublisher publisher, Symbol key, Symbol clientKey, IComputed computed, IRenewalPolicy renewalPolicy)
        {
            Publisher = publisher;
            Key = key;
            ClientKey = clientKey;
            Computed = computed;
            RenewalPolicy = renewalPolicy;
        }

        public abstract void Dispose();
    }
}
