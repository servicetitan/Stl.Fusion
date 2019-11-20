using System.Collections.Generic;
using FluentAssertions;
using Stl.Collections;
using Stl.Comparison;
using Stl.ImmutableModel;
using Stl.ImmutableModel.Indexing;
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

            idx.GetNode(Key.Parse("@")).Should().Equals(idx.Model);
            idx.GetNodeByPath(SymbolList.Empty).Should().Equals(idx.Model);
            
            var cluster1 = idx.GetNode<Cluster>(Key.Parse("cluster1"));
            cluster1.LocalKey.Value.Should().Be("cluster1");
            idx.GetPath(cluster1).FormattedValue.Should().Be("|cluster1");
            
            var vm1 = idx.GetNode<VirtualMachine>(Key.Parse("cluster1|vm1"));
            vm1.LocalKey.Value.Should().Be("vm1");
            cluster1["vm1"].Should().Equals(vm1);
            idx.GetPath(vm1).FormattedValue.Should().Be("|cluster1|vm1");
            idx.GetNodeByPath(idx.GetPath(vm1)).Should().Equals(vm1);

            var vm2 = idx.GetNode<VirtualMachine>(Key.Parse("cluster1|vm2"));
            vm2.LocalKey.Value.Should().Be("vm2");
            cluster1["vm2"].Should().Equals(vm2);
            idx.GetPath(vm2).FormattedValue.Should().Be("|cluster1|vm2");
            idx.GetNodeByPath(idx.GetPath(vm2)).Should().Equals(vm2);

            idx.GetNodeByPath<VirtualMachine>(idx.GetPath(vm1)).Capabilities
                .Should().Equals("caps1");
            idx.GetNodeByPath<VirtualMachine>(idx.GetPath(vm2)).Capabilities
                .Should().Equals("caps2");
            idx.GetNode<VirtualMachine>(vm1.Key).Should().Equals(vm1);
            idx.GetNode<VirtualMachine>(vm2.Key).Should().Equals(vm2);
        }

        [Fact]
        public void UpdateTest()
        {
            var idx = BuildModel();
            var cluster1 = idx.GetNode<Cluster>(Key.Parse("cluster1"));
            var vm2 = cluster1["vm2"];
            var vm3 = new VirtualMachine() {
                Key = vm2.Key.Parts.Prefix! + "vm3",
                Capabilities = "caps3",
            };
            
            var cluster1a = cluster1.ToUnfrozen();
            cluster1a.Remove(vm2);
            cluster1a.Add(vm3);
            cluster1a["vm3"].Should().Equals(vm3);
            var (idx1, changeSet) = idx.With(cluster1, cluster1a);
            idx = idx1;
            TestIntegrity(idx);

            var (changeSet1, json) = changeSet.PassThroughAllSerializersWithOutput();
            Out.WriteLine($"JSON: {json}");
            var c = DictionaryComparison.New(changeSet, changeSet1);
            c.AreEqual.Should().BeTrue();

            var cluster1ax = idx.GetNode<Cluster>(Key.Parse("cluster1"));
            cluster1ax.Should().Equals(cluster1a);
            cluster1ax.Should().NotEqual(cluster1);

            var vm3a = idx.GetNode<VirtualMachine>(Key.Parse("cluster1|vm3"));
            vm3a.Should().Equals(vm3);
            idx.GetPath(vm3).FormattedValue.Should().Be("|cluster1|vm3");

            // TODO: Add more tests.
        }

        internal static void TestIntegrity(IModelIndex index)
        {
            void ProcessNode(SymbolList path, INode node)
            {
                index.GetPath(node).Should().Equals(path);
                index.GetNodeByPath(path).Should().Equals(node);
                index.GetNode(node.Key).Should().Equals(node);

                var nodeTypeDef = node.GetDefinition();
                var buffer = ListBuffer<KeyValuePair<Symbol, INode>>.Lease();
                try {
                    nodeTypeDef.GetNodeItems(node, ref buffer);
                    foreach (var (k, n) in buffer)
                        ProcessNode(path + k, n);
                }
                finally {
                    buffer.Release();
                }
            }

            var root = index.Model;
            ProcessNode(SymbolList.Empty, root);
        }

        internal static ModelIndex<ModelRoot> BuildModel()
        {
            var vm1 = new VirtualMachine() {
                Key = Key.Parse("cluster1|vm1"),
                Capabilities = "caps1",
            };
            var vm2 = new VirtualMachine() {
                Key = Key.Parse("cluster1|vm2"),
                Capabilities = "caps2",
            };
            
            var cluster = new Cluster() {
                Key = Key.Parse("cluster1"),
            };
            cluster.Add(vm1);
            cluster.Add(vm2);

            var root = new ModelRoot() {
                Key = Key.Parse("@"),
            };
            root.Add(cluster);
            
            var idx = ModelIndex.New(root);
            TestIntegrity(idx);
            return idx;
        }
    }
}
