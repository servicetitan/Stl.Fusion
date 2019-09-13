using Stl.ImmutableModel;
using FluentAssertions;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel
{
    public class UpdaterTest : TestBase
    {
        public UpdaterTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
            var index = IndexTest.BuildModel();
            var updater = SimpleUpdater.New(index, index.Model);

            updater.Updated += info => {
                Out.WriteLine($"Model updated. Changes:");
                foreach (var (key, kind) in info.ChangeSet.Changes)
                    Out.WriteLine($"- {key}: {kind}");
            };

            var info = updater.Update(idx => {
                var vm1 = idx.Resolve<VirtualMachine>("./cluster1/vm1");
                return idx.Update(vm1, vm1.With(VirtualMachine.CapabilitiesSymbol, "caps1a"));
            });
            IndexTest.TestIntegrity(updater.Index);

            updater.Index.Resolve<string>(
                new SymbolPath("./cluster1/vm1") + VirtualMachine.CapabilitiesSymbol)
                .Should().Equals("caps1a");
            info.ChangeSet.Changes.Count.Equals(3);

            info = updater.Update(idx => {
                var cluster1 = idx.Resolve<Cluster>("./cluster1");
                return idx.Update(cluster1, cluster1.WithRemoved("vm1"));
            });
        }
    }
}
