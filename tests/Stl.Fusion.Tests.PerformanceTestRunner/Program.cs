// See https://aka.ms/new-console-template for more information

using Stl.Fusion.Tests;
using Stl.Reflection;

await Run<PerformanceTest_PostgreSql>();
await Run<PerformanceTest_MariaDb>();
await Run<PerformanceTest_SqlServer>();
// await Run<PerformanceTest_Sqlite>();
await Run<PerformanceTest_InMemoryDb>();

async Task Run<TTest>()
    where TTest : PerformanceTestBase
{
    var testOutputHelper = new ConsoleTestOutputHelper();
    await using var test = (TTest) typeof(TTest).CreateInstance(testOutputHelper);
    await test.InitializeAsync();
    await test.ComputedPerformanceTest();
    WriteLine("");
}
