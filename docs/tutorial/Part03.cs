using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion;
using static System.Console;

namespace Tutorial
{
    public static class Part03
    {
        #region part03_createHelper
        public static TService Create<TService>()
            where TService : class, IComputedService
        {
            var providerFactory = new DefaultServiceProviderFactory();
            var services = new ServiceCollection();
            services.AddFusionCore();
            services.AddComputedService<TService>();
            var provider = providerFactory.CreateServiceProvider(services);
            return provider.GetRequiredService<TService>();
        }
        #endregion

        #region part03_service1
        public class Service1 : IComputedService
        {
            [ComputedServiceMethod]
            public virtual async Task<DateTime> GetTimeAsync()
            {
                WriteLine($"* {nameof(GetTimeAsync)}");
                return DateTime.Now;
            }

            [ComputedServiceMethod]
            public virtual async Task<DateTime> GetTimeWithOffsetAsync(TimeSpan offset)
            {
                WriteLine($"* {nameof(GetTimeWithOffsetAsync)}({offset})");
                var time = await GetTimeAsync();
                return time + offset;
            }
        }
        #endregion

        public static async Task UseService1_Part1()
        {
            #region part03_useService1_part1
            var service1 = Create<Service1>();
            WriteLine($"{nameof(service1)}'s actual type: {service1.GetType()}");

            // You should see two methods are executed here: GetTimeAsync and GetTimeWithOffsetAsync 
            var time1 = await service1.GetTimeWithOffsetAsync(TimeSpan.FromHours(1));
            WriteLine($"{nameof(time1)}: {time1}");

            // You should see just one method is executed here: GetTimeWithOffsetAsync 
            var time2 = await service1.GetTimeWithOffsetAsync(TimeSpan.FromDays(1));
            WriteLine($"{nameof(time2)}: {time2}");
            #endregion
        }
    }
}
