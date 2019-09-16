using System.Threading.Tasks;
using Stl.ImmutableModel;
using FluentAssertions;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Updating;
using Stl.Testing;
using Stl.Tests.ImmutableModel.Indexing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel.Updating
{
    public abstract class UpdaterTestBase : TestBase
    {
        protected UpdaterTestBase(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTestAsync()
        {
            var index = IndexTest.BuildModel();
            var updater = CreateUpdater(index);

            updater.Updated += info => {
                Out.WriteLine($"Model updated. Changes:");
                foreach (var (key, kind) in info.ChangeSet.Changes)
                    Out.WriteLine($"- {key}: {kind}");
            };

            var info = await updater.UpdateAsync(idx => {
                var vm1 = idx.Resolve<VirtualMachine>("./cluster1/vm1");
                return idx.Update(vm1, vm1.With(VirtualMachine.CapabilitiesSymbol, "caps1a"));
            });
            IndexTest.TestIntegrity(updater.Index);

            updater.Index.Resolve<string>(
                new SymbolPath("./cluster1/vm1") + VirtualMachine.CapabilitiesSymbol)
                .Should().Equals("caps1a");
            info.ChangeSet.Changes.Count.Equals(3);

            info = await updater.UpdateAsync(idx => {
                var cluster1 = idx.Resolve<Cluster>("./cluster1");
                return idx.Update(cluster1, cluster1.WithRemoved("vm1"));
            });
        }

        protected abstract IUpdater<UpdatableIndex<ModelRoot>, ModelRoot> CreateUpdater(UpdatableIndex<ModelRoot> index);
    }
}
