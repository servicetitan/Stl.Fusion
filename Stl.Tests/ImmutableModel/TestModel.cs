using System;
using System.Runtime.Serialization;
using Stl.ImmutableModel;

namespace Stl.Tests.ImmutableModel
{
    [Serializable]
    public class ModelRoot : CollectionNodeBase<Cluster>
    {
        public ModelRoot(Key key) : base(key) { }
        public ModelRoot(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }

    [Serializable]
    public class Cluster : CollectionNodeBase<VirtualMachine>
    {
        public Cluster(Key key) : base(key) { }
        public Cluster(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }

    [Serializable]
    public class VirtualMachine : SimpleNodeBase
    {
        public static readonly Symbol CapabilitiesSymbol = new Symbol(nameof(Capabilities));
        public string Capabilities => (string) this[CapabilitiesSymbol]!;

        public VirtualMachine(Key key) : base(key) { }
        public VirtualMachine(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}
