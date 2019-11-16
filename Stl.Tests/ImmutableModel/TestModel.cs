using System;
using System.Runtime.Serialization;
using Stl.ImmutableModel;

namespace Stl.Tests.ImmutableModel
{
    [Serializable]
    public class ModelRoot : CollectionNode<Cluster>
    { }

    [Serializable]
    public class Cluster : CollectionNode<VirtualMachine>
    { }

    [Serializable]
    public class VirtualMachine : SimpleNodeBase
    {
        private string _capabilities;

        public string Capabilities {
            get => _capabilities;
            set => _capabilities = PrepareValue(nameof(Capabilities), value);
        }
    }
}
