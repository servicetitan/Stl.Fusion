using System;
using System.Diagnostics;
using System.Threading;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Reactionist.Tests
{
    public class VarTest : TestBase
    {
        public VarTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void BasicTest()
        {
            var v1 = Var.New(0);
            Assert.Equal(0, v1.Value);

            var notificationCount = 0;
            var reaction = new Reaction((_, e) => {
                notificationCount++;
                Assert.IsType<ChangedEventData>(e.Data);
                Assert.Same(e.Source, v1);
            });

            Assert.True(v1.AddReaction(reaction));
            
            v1.Value = 1;
            Assert.Equal(1, v1.Value);
            Assert.Equal(1, notificationCount);
            
            Assert.True(v1.RemoveReaction(reaction));
            v1.Value = 2;
            Assert.Equal(2, v1.Value);
            Assert.Equal(1, notificationCount);
            
            Var<string> v2;
            var dt = new DependencyTracker();
            using (dt.Activate()) {
                v1.Value = 3;
                // ReSharper disable once RedundantAssignment
                v2 = Var.New("X");
            }
            Assert.Equal(dt.Dependencies, new ReactiveBase[] {});
            
            dt = new DependencyTracker();
            using (dt.Activate()) {
                // ReSharper disable once UnusedVariable
                // ReSharper disable once InconsistentNaming
                var _ignored1 = v1.Value;
                v2 = new Var<string>("X");
                // ReSharper disable once UnusedVariable
                // ReSharper disable once InconsistentNaming
                var _ignored2 = v2.Value;
            }
            Assert.Equal(dt.Dependencies, new ReactiveBase[] {v1, v2});
        }
        
        [Fact]
        public void ComputedVarTest1()
        {
            var va = Var.New(1);
            var vb = Var.New(2);
            var vc = ComputedVar.New(() => va.Value + vb.Value);
            
            bool checksPassed = true;
            vc.AddReaction(new Reaction((_, e) => {
                checksPassed &= !Thread.CurrentThread.IsThreadPoolThread;
            }));

            Assert.Equal(3, vc.Value);
            
            va.Value = 3;
            Assert.Equal(5, vc.Value);
            
            vb.Value = 4;
            Assert.Equal(7, vc.Value);
            Assert.True(checksPassed);
        }
        
        [Fact]
        public void ComputedVarTest2()
        {
            var va = Var.New(1); 
            var vb = Var.New(10);
            // ReSharper disable once InconsistentNaming
            var useVB = false;
            // ReSharper disable once InconsistentNaming
            Var<int> GetVAOrVB() => useVB ? vb : va;

            var vc = ComputedVar.New(() => GetVAOrVB() + vb);
            var vd = ComputedVar.New(() => vc + 1);

            Assert.Equal(11, vc.Value);
            Assert.Equal(12, vd.Value);
    
            va.Value = 2;
            Assert.Equal(12, vc.Value);
            Assert.Equal(13, vd.Value);
        
            useVB = true;
            vc.Invalidate();
            // GetVAOrVB should make sure vb is returned instead of va on recompute
            Assert.Equal(20, vc.Value);
            Assert.Equal(21, vd.Value);
        }
        
        [Theory]
        [InlineData(1_000)]
        [InlineData(1_001)]
        [InlineData(10_000)]
        [InlineData(100_000)]
        [InlineData(1_000_000)]
        public void ComputedVarSpeedTest(int iterationCount)
        {
            var va = Var.New(1); 
            var vb = Var.New(10);
            var vc = ComputedVar.New(() => va + vb);
            var vd = ComputedVar.New(() => vc + 1);

            void Test(int count) {
                var c = count; // Just to speed up the loop itself
                for (var i = 0; i < c; i++)
                    va.Value = i;
                var expected = va + vb + 1;
                Assert.Equal(expected, vd.Value);
            }
            Test(1000); // Warmup

            var sw = new Stopwatch();
            var ram = (double) GC.GetTotalMemory(true);
            sw.Start();
            Test(iterationCount);
            sw.Stop();
            ram = Math.Max(0, GC.GetTotalMemory(false) - ram);
            Out.WriteLine($"Duration:   {sw.Elapsed.TotalMilliseconds} ms");
            Out.WriteLine($"Iterations: {iterationCount}");
            Out.WriteLine($"Speed:      {iterationCount / sw.Elapsed.TotalSeconds / 1_000_000:F2} M ops/s");
            Out.WriteLine($"Allocated:  {ram / 1024} KB ({ram / iterationCount:F2} bytes/op)");
        }
        
        /*
        
        [Fact]
        public void DelayingSchedulerTest()
        {
            var va = Var.New(1);
            var vb = Var.New(2);

            var vcTmp = ComputedVar.New(() => va.Value + vb.Value);
            Assert.Equal(3, vcTmp.Value);
            vcTmp.Dispose();
            Assert.False(vcTmp.IsComputed);
            Assert.Throws<ObjectDisposedException>(() => vcTmp.Value);

            var vc = ComputedVar.New(() => va.Value + vb.Value, ComputedVar.DelayingScheduler);
            Assert.Equal(3, vc.Value);
            va.Value = 2;
            Assert.Equal(3, vc.Value);
            Thread.Sleep(50);
            Assert.Equal(4, vc.Value);
        }
        
        [Fact]
        public void NewDelayingSchedulerTest()
        {
            var va = Var.New(1);
            var vb = Var.New(2);
            var vc = ComputedVar.New(() => va.Value + vb.Value,
                ComputedVar.NewDelayingScheduler(TimeSpan.FromMilliseconds(100)));
            Assert.Equal(3, vc.Value);
            
            va.Value = 2;
            Assert.Equal(3, vc.Value);
            
            Thread.Sleep(50);
            Assert.Equal(3, vc.Value);
            
            Thread.Sleep(150);
            Assert.Equal(4, vc.Value);
        }

        
        [Fact]
        public void NewThreadPoolSchedulerTest()
        {
            var va = Var.New(1);
            var vb = Var.New(2);
            var vc = ComputedVar.New(() => va.Value + vb.Value, ComputedVar.ThreadPoolScheduler);
            
            bool checksPassed = true;
            vc.AddListener(new Listener((_, e) => {
                checksPassed &= Thread.CurrentThread.IsThreadPoolThread;
            }));
            Assert.Equal(3, vc.Value);
            
            va.Value = 2;
            Assert.Equal(3, vc.Value);
            
            Thread.Sleep(50);
            Assert.Equal(4, vc.Value);
            
            Assert.True(checksPassed);
        }
        
        */
    }
}
