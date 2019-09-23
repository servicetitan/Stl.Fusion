using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using FluentAssertions;
using System.Threading.Tasks;
using Stl.ImmutableModel;
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
            using var changeTracker = ChangeTracker.New(updater);

            using var o = changeTracker.ChangesIncluding(index.Model.Key, NodeChangeType.Any).Subscribe(
                Observer.Create<UpdateInfo<ModelRoot>>(updateInfo => {
                    Out.WriteLine($"Model updated. Changes:");
                    foreach (var (key, kind) in updateInfo.ChangeSet.Changes)
                        Out.WriteLine($"- {key}: {kind}");
                }));
            var c1task = changeTracker.AllChanges
                .Select((_, i) => i).ToTask();
            var c2task = changeTracker.ChangesIncluding(index.Model.Key, NodeChangeType.Any)
                .Select((_, i) => i).ToTask();

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

            changeTracker.Dispose();
            (await c1task).Should().Equals(await c2task);
        }

        protected abstract IUpdater<ModelRoot> CreateUpdater(IUpdatableIndex<ModelRoot> index);
    }
}
