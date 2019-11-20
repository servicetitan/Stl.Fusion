using System;
using System.Runtime.Serialization;
using Stl.ImmutableModel;

namespace Stl.Tests.ImmutableModel
{
    public class ModelRoot : CollectionNode<Cluster>
    { }

    public class Cluster : CollectionNode<VirtualMachine>
    { }

    public class VirtualMachine : SimpleNodeBase
    {
        private string _capabilities = "";

        public string Capabilities {
            get => _capabilities;
            set => _capabilities = PrepareValue(nameof(Capabilities), value);
        }
    }
}
