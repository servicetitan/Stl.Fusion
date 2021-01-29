using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using Templates.Blazor2.Host;

var host = Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(builder => {
        // Looks like there is no better way to set _default_ URL
        builder.Sources.Insert(0, new MemoryConfigurationSource() {
            InitialData = new Dictionary<string, string>() {
                {WebHostDefaults.ServerUrlsKey, "http://localhost:5005"},
            }
        });
    })
    .ConfigureWebHostDefaults(builder => builder
        .UseDefaultServiceProvider((ctx, options) => {
            if (ctx.HostingEnvironment.IsDevelopment()) {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            }
        })
        .UseStartup<Startup>())
    .Build();

// Ensure the DB is created
/*
var dbContextFactory = host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
await using var dbContext = dbContextFactory.CreateDbContext();
await dbContext.Database.EnsureCreatedAsync();
*/

await host.RunAsync();
