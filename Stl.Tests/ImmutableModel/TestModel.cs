using Stl.ImmutableModel;

namespace Stl.Tests.ImmutableModel
{
    public interface IHasCapabilities
    {
        string Capabilities { get; set; }
    }

    public class ModelRoot : CollectionNode<Cluster>
    { }

    public class Cluster : CollectionNode<VirtualMachine>, IHasCapabilities
    {
        private string _capabilities = "";

        [NodeProperty(IsNodeProperty = false)]
        public string NotAProperty { get; set; } = "";

        public string Capabilities {
            get => _capabilities;
            set => _capabilities = PreparePropertyValue(nameof(Capabilities), value);
        }
    }

    public class VirtualMachine : Node, IHasCapabilities
    {
        private string _capabilities = "";

        public string Capabilities {
            get => _capabilities;
            set => _capabilities = PreparePropertyValue(nameof(Capabilities), value);
        }
    }
}
