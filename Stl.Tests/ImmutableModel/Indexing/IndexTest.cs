using FluentAssertions;
using Stl.Comparison;
using Stl.ImmutableModel;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Updating;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel.Indexing
{
    public class IndexTest : TestBase
    {
        public IndexTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void IndexingTest()
        {
            var idx = BuildModel();
            var (tmpIdx, json) = idx.PassThroughAllSerializersWithOutput();
            Out.WriteLine($"JSON: {json}");
            idx = tmpIdx;

            idx.Resolve(".").Should().Equals(idx.Model);
            
            var cluster1 = idx.Resolve<Cluster>("./cluster1");
            cluster1.LocalKey.Value.Should().Be("cluster1");
            idx.GetPath(cluster1).Value.Should().Be("./cluster1");
            
            var vm1 = idx.Resolve<VirtualMachine>("./cluster1/vm1");
            vm1.LocalKey.Value.Should().Be("vm1");
            cluster1["vm1"].Should().Equals(vm1);
            idx.GetPath(vm1).Value.Should().Be("./cluster1/vm1");

            var vm2 = idx.Resolve<VirtualMachine>("./cluster1/vm2");
            vm2.LocalKey.Value.Should().Be("vm2");
            cluster1["vm2"].Should().Equals(vm2);
            idx.GetPath(vm2).Value.Should().Be("./cluster1/vm2");

            idx.Resolve(idx.GetPath(vm1) + new Symbol(VirtualMachine.CapabilitiesSymbol))
                .Should().Equals("caps1");
            idx.Resolve(idx.GetPath(vm2) + new Symbol(VirtualMachine.CapabilitiesSymbol))
                .Should().Equals("caps2");
            idx.GetNode<VirtualMachine>(vm1.Key).Should().Equals(vm1);
            idx.GetNode<VirtualMachine>(vm2.Key).Should().Equals(vm2);
        }

        [Fact]
        public void UpdateTest()
        {
            var idx = BuildModel();
            var cluster1 = idx.Resolve<Cluster>("./cluster1");
            var vm2 = cluster1["vm2"];
            var vm3 = new VirtualMachine(vm2.Key.Path.Head! + "vm3")
                .With(VirtualMachine.CapabilitiesSymbol, "caps3");
            
            var cluster1a = cluster1.WithRemoved(vm2).WithAdded(vm3);
            cluster1a["vm3"].Should().Equals(vm3);
            var (idx1, changeSet) = idx.Update(cluster1, cluster1a);
            idx = idx1;
            TestIntegrity(idx);

            var (changeSet1, json) = changeSet.PassThroughAllSerializersWithOutput();
            Out.WriteLine($"JSON: {json}");
            var c = DictionaryComparison.New(changeSet.Changes, changeSet1.Changes);
            c.AreEqual.Should().BeTrue();

            var cluster1ax = idx.Resolve<Cluster>("./cluster1");
            cluster1ax.Should().Equals(cluster1a);
            cluster1ax.Should().NotEqual(cluster1);

            var vm3a = idx.Resolve<VirtualMachine>("./cluster1/vm3");
            vm3a.Should().Equal(vm3);
            idx.GetPath(vm3).Value.Should().Be("./cluster1/vm3");

            // TODO: Add more tests.
        }

        internal static void TestIntegrity(IIndex index)
        {
            void ProcessNode(SymbolPath path, INode node)
            {
                index.GetPath(node).Should().Equals(path);
                index.GetNode(path).Should().Equals(node);
                index.GetNode(node.Key).Should().Equals(node);

                foreach (var (k, n) in node.DualGetNodeItems())
                    ProcessNode(path + k, n);
            }

            var root = index.UntypedModel;
            ProcessNode(root.LocalKey.Value, root);
        }

        internal static UpdatableIndex<ModelRoot> BuildModel()
        {
            var vm1 = new VirtualMachine(new Key("./cluster1/vm1"))
                .With(VirtualMachine.CapabilitiesSymbol, "caps1");
            var vm2 = new VirtualMachine(new Key("./cluster1/vm2"))
                .With(VirtualMachine.CapabilitiesSymbol, "caps2");
            var cluster = new Cluster(new Key("cluster1"))
                .WithAdded(vm1, vm2);
            var root = new ModelRoot(new Key("."))
                .WithAdded(cluster);
            
            var idx = UpdatableIndex.New(root);
            TestIntegrity(idx);
            return idx;
        }
    }
}
