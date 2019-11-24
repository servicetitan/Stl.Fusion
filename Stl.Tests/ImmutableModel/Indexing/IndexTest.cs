using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stl.Collections;
using Stl.Comparison;
using Stl.ImmutableModel;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Reflection;
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
            var (tmpIdx, json) = idx.PassThroughJsonConvertWithOutput();
            Out.WriteLine($"JSON: {json}");
            idx = tmpIdx;

            idx.GetNode(Key.Parse("@")).Should().Equals(idx.Model);
            idx.GetNode(NodeLink.Null).Should().Equals(idx.Model);
            
            var cluster1 = idx.GetNode<Cluster>(Key.Parse("cluster1"));
            cluster1.Key.Format().Should().Be("cluster1");
            idx.GetNodeLink(cluster1).Should().Be(new NodeLink(idx.Model.Key, cluster1.Key));

            cluster1.Capabilities.Should().Equals("huge");
            cluster1.NotAProperty.Should().Equals("true");
            
            var clusterDef = cluster1.GetDefinition();
            clusterDef.Properties.Count.Should().Be(1);
            clusterDef.NodeProperties.Count.Should().Be(0);

            Out.WriteLine($"All of {cluster1} items:");
            var cluster1Items = clusterDef.GetAllItems(cluster1).ToList();
            foreach (var (itemKey, value) in cluster1Items)
                Out.WriteLine($"- {itemKey} -> {value}");
            cluster1Items.Count.Should().Be(5);
            
            var vm1 = idx.GetNode<VirtualMachine>(Key.Parse("vm1|cluster1"));
            vm1.Key.Format().Should().Be("vm1|cluster1");
            cluster1[vm1.Key].Should().Equals(vm1);
            idx.GetNodeLink(vm1).Should().Be(new NodeLink(cluster1.Key, vm1.Key));
            idx.GetNode(idx.GetNodeLink(vm1)).Should().Equals(vm1);

            var vm2 = idx.GetNode<VirtualMachine>(Key.Parse("vm2|cluster1"));
            vm2.Key.Format().Should().Be("vm2|cluster1");
            cluster1[vm2.Key].Should().Equals(vm2);
            idx.GetNodeLink(vm2).Should().Be(new NodeLink(cluster1.Key, vm2.Key));
            idx.GetNode(idx.GetNodeLink(vm2)).Should().Equals(vm2);

            idx.GetNode<VirtualMachine>(idx.GetNodeLink(vm1)).Capabilities
                .Should().Equals("caps1");
            idx.GetNode<VirtualMachine>(idx.GetNodeLink(vm2)).Capabilities
                .Should().Equals("caps2");
            idx.GetNode<VirtualMachine>(vm1.Key).Should().Equals(vm1);
            idx.GetNode<VirtualMachine>(vm2.Key).Should().Equals(vm2);
        }

        [Fact]
        public void UpdateTest()
        {
            var idx = BuildModel();
            var cluster1 = idx.GetNode<Cluster>(Key.Parse("cluster1"));
            var vm2 = cluster1[Key.Parse("vm2|cluster1")];
            var vm3 = new VirtualMachine() {
                Key = "vm3" & vm2.Key.Continuation,
                Capabilities = "caps3",
            };
            
            var cluster1a = cluster1.ToUnfrozen();
            cluster1a.Remove(vm2);
            cluster1a.Add(vm3);
            cluster1a[Key.Parse("vm3|cluster1")].Should().Equals(vm3);
            var (idx1, changeSet) = idx.With(cluster1, cluster1a);
            idx = idx1;
            TestIntegrity(idx);

            var (changeSet1, json) = changeSet.PassThroughJsonConvertWithOutput();
            Out.WriteLine($"JSON: {json}");
            var c = DictionaryComparison.New(changeSet, changeSet1);
            c.AreEqual.Should().BeTrue();

            var cluster1ax = idx.GetNode<Cluster>(Key.Parse("cluster1"));
            cluster1ax.Should().Equals(cluster1a);
            cluster1ax.Should().NotEqual(cluster1);

            var vm3a = idx.GetNode<VirtualMachine>(Key.Parse("vm3|cluster1"));
            vm3a.Should().Equals(vm3);
            idx.GetNodeLink(vm3).Should().Be(new NodeLink(cluster1.Key, vm3.Key));

            // TODO: Add more tests.
        }

        internal static void TestIntegrity(IModelIndex index)
        {
            void ProcessNode(NodeLink nodeLink, INode node)
            {
                index.GetNodeLink(node).Should().Equals(nodeLink);
                index.GetNode(nodeLink).Should().Equals(node);
                index.GetNode(node.Key).Should().Equals(node);

                var nodeTypeDef = node.GetDefinition();
                var buffer = ListBuffer<KeyValuePair<ItemKey, INode>>.Lease();
                try {
                    nodeTypeDef.GetNodeItems(node, ref buffer);
                    var parentKey = node.Key;
                    foreach (var (itemKey, n) in buffer)
                        ProcessNode((parentKey, itemKey), n);
                }
                finally {
                    buffer.Release();
                }
            }

            var root = index.Model;
            ProcessNode(NodeLink.Null, root);
        }

        internal static ModelIndex<ModelRoot> BuildModel()
        {
            var vm1 = new VirtualMachine() {
                Key = Key.Parse("vm1|cluster1"),
                Capabilities = "caps1",
            };
            var vm2 = new VirtualMachine() {
                Key = Key.Parse("vm2|cluster1"),
                Capabilities = "caps2",
            };
            
            var cluster = new Cluster() {
                Key = Key.Parse("cluster1"),
                Capabilities = "huge",
                NotAProperty = "true"
            };
            cluster.SetOption("@o1", "v1");
            cluster.SetOption("@o2", "v2");
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
