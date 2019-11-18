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
            await using var updater = CreateModelUpdater(index);
            var changeTracker = updater.ChangeTracker;

            using var o = changeTracker.ChangesIncluding(index.Model.Key, NodeChangeType.Any).Subscribe(
                Observer.Create<ModelUpdateInfo<ModelRoot>>(updateInfo => {
                    Out.WriteLine($"Model updated. Changes:");
                    foreach (var (key, kind) in updateInfo.ChangeSet.Items)
                        Out.WriteLine($"- {key}: {kind}");
                }));
            var c1task = changeTracker.AllChanges
                .Select((_, i) => i).ToTask();
            var c2task = changeTracker.ChangesIncluding(index.Model.Key, NodeChangeType.Any)
                .Select((_, i) => i).ToTask();

            var info = await updater.UpdateAsync(idx => {
                var vm1 = idx.GetNode<VirtualMachine>(Key.Parse("cluster1|vm1"));
                var vm1c = vm1.ToUnfrozen();
                vm1c.Capabilities = "caps1a"; 
                return idx.With(vm1, vm1c);
            });
            IndexTest.TestIntegrity(updater.Index);

            updater.Index.GetNode<VirtualMachine>(Key.Parse("cluster1|vm1")).Capabilities
                .Should().Equals("caps1a");
            info.ChangeSet.Count.Equals(3);

            info = await updater.UpdateAsync(idx => {
                var cluster1 = idx.GetNode<Cluster>(Key.Parse("cluster1"));
                var cluster1c = cluster1.ToUnfrozen();
                cluster1c.Remove("vm1");
                return idx.With(cluster1, cluster1c);
            });

            changeTracker.Dispose();
            (await c1task).Should().Equals(await c2task);
        }

        protected abstract IModelUpdater<ModelRoot> CreateModelUpdater(IModelIndex<ModelRoot> index);
    }
}
