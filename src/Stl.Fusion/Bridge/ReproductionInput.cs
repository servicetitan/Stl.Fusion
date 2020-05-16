using System;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public class ReproductionInput : ComputedInput, IEquatable<ReproductionInput>
    {
        protected internal IReproductionImpl ReproductionImpl { get; }
        // Shortcuts
        public IReproducer Reproducer => ReproductionImpl.Reproducer;
        public Symbol PublisherId => ReproductionImpl.PublisherId;
        public Symbol PublicationId => ReproductionImpl.PublicationId;
        public IReproduction Reproduction => ReproductionImpl;

        public ReproductionInput(IReproductionImpl reproductionImpl) 
            : base(reproductionImpl) 
            => ReproductionImpl = reproductionImpl;

        public override string ToString() 
            => $"{GetType().Name}({Reproduction.PublicationId})";

        // Equality

        public bool Equals(ReproductionInput? other) 
            => !ReferenceEquals(null, other) && PublicationId == other.PublicationId;
        public override bool Equals(ComputedInput other)
            => other is ReproductionInput ri && PublicationId == ri.PublicationId;
        public override bool Equals(object? obj) 
            => Equals(obj as ReproductionInput);
        public override int GetHashCode() 
            => PublicationId.GetHashCode();
        public static bool operator ==(ReproductionInput? left, ReproductionInput? right) 
            => Equals(left, right);
        public static bool operator !=(ReproductionInput? left, ReproductionInput? right) 
            => !Equals(left, right);
    }
}
