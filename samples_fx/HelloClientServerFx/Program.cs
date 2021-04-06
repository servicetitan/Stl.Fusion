using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using Microsoft.Owin.Hosting;
using Stl.Async;
using Stl.Fusion;
using Stl.Fusion.Client;

namespace HelloClientServerFx
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string baseAddress = "http://localhost:9001/"; 

            // Start OWIN host 
            using (WebApp.Start(url: baseAddress, new Startup().Configuration)) 
            { 
                //Test(baseAddress);

                await Test2(baseAddress);

                Console.WriteLine("Press ENTER to exit.");

                Console.ReadLine();
            } 
        }

        private static async Task Test2(string baseAddress)
        {
            using var stopCts = new CancellationTokenSource();
            var cancellationToken = stopCts.Token;
            
            var services = CreateClientServices(baseAddress);
            var counters = services.GetRequiredService<ICounterService>();
            
            async Task Watch<T>(string name, IComputed<T> computed)
            {
                for (; ; )
                {
                    Console.WriteLine($"{name}: {computed.Value}, {computed}");
                    await computed.WhenInvalidated(cancellationToken);
                    Console.WriteLine($"{name}: {computed.Value}, {computed}");
                    computed = await computed.Update(false, cancellationToken);
                }
            }
            
            var aComputed = await Computed.Capture(_ => counters.Get("a"));
            Task.Run(() => Watch(nameof(aComputed), aComputed)).Ignore();
            var bComputed = await Computed.Capture(_ => counters.Get("b"));
            Task.Run(() => Watch(nameof(bComputed), bComputed)).Ignore();

            await Task.Delay(1000);
            await counters.Increment("a");
            await Task.Delay(200);
            await counters.SetOffset(10);
            await Task.Delay(200);
            
            stopCts.Cancel();
        }

        private static void Test(string baseAddress)
        {
            // Create HttpClient and make a request to api/values 
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add(FusionHeaders.RequestPublication, "true");
            Test(client, baseAddress);
        }

        private static void Test(HttpClient client, string baseAddress)
        {
            var response = client.GetAsync(baseAddress + "api/counter/xxx").Result;
            Console.WriteLine(response);
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
        }
        
        private static IServiceProvider CreateClientServices(string baseUri)
        {
            var services = new ServiceCollection();
            
            //ConfigureClientServicesLogging(services);
            
            ConfigureClientServices(services, baseUri);
            return services.BuildServiceProvider();
        }

        private static void ConfigureClientServicesLogging(ServiceCollection services) =>
            services.AddLogging(c => {
                c.ClearProviders();
                c.AddConsole();
            });

        private static void ConfigureClientServices(IServiceCollection services, string baseUri)
        {
            var apiBaseUri = new Uri($"{baseUri}api/");
            services.ConfigureAll<HttpClientFactoryOptions>(options => {
                // Replica Services construct HttpClients using IHttpClientFactory, so this is
                // the right way to make all HttpClients to have BaseAddress = apiBaseUri by default.
                options.HttpClientActions.Add(client => client.BaseAddress = apiBaseUri);
            });
            var fusion = services.AddFusion();
            var fusionClient = fusion.AddRestEaseClient((c, options) => options.BaseUri = new Uri(baseUri));
            fusionClient.AddReplicaService<ICounterService, ICounterServiceClient>();
        }
    }
}
