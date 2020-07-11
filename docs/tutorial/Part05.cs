using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl;
using Stl.Fusion;
using static System.Console;

namespace Tutorial
{
    public static class Part05
    {
        public static Task DefineServices() => Task.CompletedTask;
        #region part05_defineServices
        public class UserRegistry : IComputedService
        {
            private readonly ConcurrentDictionary<long, string> _userNames = 
                new ConcurrentDictionary<long, string>();

            // Notice there is no [ComputedServiceMethod], because it doesn't
            // return anything on which other methods may depend
            public void SetUserName(long userId, string value)
            {
                _userNames[userId] = value;
                Computed.Invalidate(() => GetUserNameAsync(userId));
                WriteLine($"! {nameof(GetUserNameAsync)}({userId}) -> invalidated");
            }

            [ComputedServiceMethod]
            public virtual async Task<string?> GetUserNameAsync(long userId)
            {
                WriteLine($"* {nameof(GetUserNameAsync)}({userId})");
                return _userNames.TryGetValue(userId, out var name) ? name : null;
            }
        }

        public class Clock : IComputedService
        {
            // A better way to implement auto-invalidation:
            // uncomment the next line and comment out the line with "Task.Delay".
            // [ComputedServiceMethod(AutoInvalidateTime = 0.1)]
            [ComputedServiceMethod]
            public virtual async Task<DateTime> GetTimeAsync()
            {
                WriteLine($"* {nameof(GetTimeAsync)}()");
                // That's how you "pull" the computed that is going to
                // store the result of this computation
                var computed = Computed.GetCurrent();
                // We just start this task here, but don't await for its result
                Task.Delay(TimeSpan.FromSeconds(0.1)).ContinueWith(_ => {
                    computed!.Invalidate();
                    WriteLine($"! {nameof(GetTimeAsync)}() -> invalidated");
                }).Ignore();
                return DateTime.Now;
            }
        }

        public class FormatService : IComputedService
        {
            private readonly UserRegistry _users;
            private readonly Clock _clock;

            public FormatService(UserRegistry users, Clock clock)
            {
                _users = users;
                _clock = clock;
            }

            [ComputedServiceMethod]
            public virtual async Task<string> FormatUserNameAsync(long userId)
            {
                WriteLine($"* {nameof(FormatUserNameAsync)}({userId})");
                var userName = await _users.GetUserNameAsync(userId);
                var time = await _clock.GetTimeAsync();
                return $"{time:HH:mm:ss:fff}: User({userId})'s name is '{userName}'";
            }
        }
        #endregion

        #region part05_createServiceProvider
        public static IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection()
                .AddFusionCore()
                .AddComputedService<UserRegistry>()
                .AddComputedService<Clock>()
                .AddComputedService<FormatService>();
            return new DefaultServiceProviderFactory().CreateServiceProvider(services);
        }
        #endregion

        public static async Task UseServices_Part1()
        {
            #region part05_useServices_part1
            var services = CreateServiceProvider();
            var users = services.GetRequiredService<UserRegistry>();
            var clock = services.GetRequiredService<Clock>();
            var formatter = services.GetRequiredService<FormatService>();

            users.SetUserName(0, "John Carmack");
            for (var i = 0; i < 5; i++) {
                WriteLine(await formatter.FormatUserNameAsync(0));
                await Task.Delay(100);
            }
            users.SetUserName(0, "Linus Torvalds");
            WriteLine(await formatter.FormatUserNameAsync(0));
            users.SetUserName(0, "Satoshi Nakamoto");
            WriteLine(await formatter.FormatUserNameAsync(0));
            #endregion
        }

        public static async Task UseServices_Part2()
        {
            #region part05_useServices_part2
            var services = CreateServiceProvider();
            var users = services.GetRequiredService<UserRegistry>();
            var clock = services.GetRequiredService<Clock>();
            var formatter = services.GetRequiredService<FormatService>();

            users.SetUserName(0, "John Carmack");
            var cFormattedUser0 = await Computed.CaptureAsync(async _ => 
                await formatter.FormatUserNameAsync(0));
            for (var i = 0; i < 10; i++) {
                WriteLine(cFormattedUser0.Value);
                await cFormattedUser0.InvalidatedAsync();
                // Note that nothing gets recomputed automatically;
                // on a positive side, any IComputed knows how to recompute itself,
                // so you can always do this manually:
                cFormattedUser0 = await cFormattedUser0.UpdateAsync(false);
            }
            #endregion
        }
    }
}
