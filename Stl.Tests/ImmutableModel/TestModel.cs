using System;
using System.Runtime.Serialization;
using Stl.ImmutableModel;

namespace Stl.Tests.ImmutableModel
{
    [Serializable]
    public class ModelRoot : CollectionNodeBase<Cluster>
    {
        public ModelRoot(LocalKey localKey) : base(localKey) { }
        public ModelRoot(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class Cluster : CollectionNodeBase<VirtualMachine>
    {
        public Cluster(LocalKey localKey) : base(localKey) { }
        public Cluster(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class VirtualMachine : SimpleNodeBase
    {
        public static readonly Symbol CapabilitiesSymbol = new Symbol(nameof(Capabilities));
        public string Capabilities => (string) this[CapabilitiesSymbol]!;

        public VirtualMachine(LocalKey localKey) : base(localKey)
        {
            Items = Items.Add(CapabilitiesSymbol, "");
        }

        public VirtualMachine(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
