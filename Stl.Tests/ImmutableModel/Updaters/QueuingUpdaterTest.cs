using Stl.ImmutableModel;
using Stl.ImmutableModel.Updaters;
using Xunit.Abstractions;

namespace Stl.Tests.ImmutableModel.Updaters
{
    public class QueuingUpdaterTest : UpdaterTestBase
    {
        public QueuingUpdaterTest(ITestOutputHelper @out) : base(@out) { }

        protected override IUpdater<Index<ModelRoot>, ModelRoot> CreateUpdater(Index<ModelRoot> index) 
            => QueuingUpdater.New(index, index.Model);
    }
}