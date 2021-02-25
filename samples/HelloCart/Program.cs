using System.Threading;
using System.Threading.Tasks;
using Samples.HelloCart;
using Samples.HelloCart.V1;
using Samples.HelloCart.V2;
using Samples.HelloCart.V3;
using Samples.HelloCart.V4;
using Samples.HelloCart.V5;
using Stl.Async;
using Stl.CommandR;
using static System.Console;

// Create services
AppBase app = new AppV1();
await using var appDisposable = app;
await app.InitializeAsync();

// Starting watch tasks
WriteLine("Initial state:");
using var cts = new CancellationTokenSource();
var task = app.WatchAsync(cts.Token);
await Task.Delay(600); // Just to make sure watch tasks print whatever they want before our prompt appears
await MiniTest.RunAsync(app);
