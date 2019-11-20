using System;
using System.Runtime.Serialization;
using Stl.ImmutableModel;

namespace Stl.Tests.ImmutableModel
{
    [Serializable]
    public class ModelRoot : CollectionNode<Cluster>
    {
        public ModelRoot() { }
        protected ModelRoot(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class Cluster : CollectionNode<VirtualMachine>
    {
        public Cluster() { }
        protected Cluster(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class VirtualMachine : SimpleNodeBase
    {
        private string _capabilities = "";

        public string Capabilities {
            get => _capabilities;
            set => _capabilities = PrepareValue(nameof(Capabilities), value);
        }

        public VirtualMachine() { }
        public VirtualMachine(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
