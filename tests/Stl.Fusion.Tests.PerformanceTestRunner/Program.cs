// See https://aka.ms/new-console-template for more information

using Stl.Fusion.Tests;
using Stl.Reflection;

if (HasSwitch("PostgreSql") || HasSwitch("npgsql") || HasSwitchAll())
    await Run<PerformanceTest_PostgreSql>();
if (HasSwitch("mariadb") || HasSwitch("mysql") || HasSwitchAll())
    await Run<PerformanceTest_MariaDb>();
if (HasSwitch("sqlserver") || HasSwitch("mssql") || HasSwitchAll())
    await Run<PerformanceTest_SqlServer>();
if (HasSwitch("sqlite") || HasSwitchAll())
    await Run<PerformanceTest_Sqlite>();
if (HasSwitch("inmemory") || HasSwitch("memory") || HasSwitchAll())
    await Run<PerformanceTest_InMemoryDb>();
WriteLine("Press any key to exit...");
ReadKey();

async Task Run<TTest>()
    where TTest : PerformanceTestBase
{
    var testOutputHelper = new ConsoleTestOutputHelper();
    await using var test = (TTest)typeof(TTest).CreateInstance(testOutputHelper);
    test.IsConsoleApp = true;
    test.UseOperationLogChangeTracking = false;
    test.UseEntityResolver = HasSwitch("-useEntityResolver") || HasSwitch("-er");
    await test.InitializeAsync();
    await test.ComputedPerformanceTest();
    WriteLine("");
}

bool HasSwitchAll()
    => HasSwitch("all");

bool HasSwitch(string name)
    => args.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
