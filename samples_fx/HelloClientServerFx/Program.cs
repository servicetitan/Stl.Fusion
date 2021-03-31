using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Stl.Fusion.Client;

namespace HelloClientServerFx
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9000/"; 

            // Start OWIN host 
            using (WebApp.Start(url: baseAddress, new Startup().Configuration)) 
            { 
                // Create HttpClient and make a request to api/values 
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add(FusionHeaders.RequestPublication, "true");

                Test(client, baseAddress);
                
                Console.WriteLine("Press ENTER to exit.");

                Console.ReadLine(); 
            } 
        }
        
        private static void Test(HttpClient client, string baseAddress)
        {
            
            var response = client.GetAsync(baseAddress + "api/counter/xxx").Result;

            Console.WriteLine(response);
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
        }
    }
}
