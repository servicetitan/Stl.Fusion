using System;
using System.Runtime.Serialization;
using Stl.ImmutableModel;

namespace Stl.Tests.ImmutableModel
{
    [Serializable]
    public class ModelRoot : CollectionNode<Cluster>
    {
        public ModelRoot(Key key) : base(key) { }
        protected ModelRoot(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class Cluster : CollectionNode<VirtualMachine>
    {
        public Cluster(Key key) : base(key) { }
        protected Cluster(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class VirtualMachine : SimpleNodeBase
    {
        public static readonly Symbol CapabilitiesSymbol = new Symbol(nameof(Capabilities));
        public string Capabilities => (string) this[CapabilitiesSymbol]!;

        public VirtualMachine(Key key) : base(key) 
            => Items = Items.Add(CapabilitiesSymbol, "");
        protected VirtualMachine(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
