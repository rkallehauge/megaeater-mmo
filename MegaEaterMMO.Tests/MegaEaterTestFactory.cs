using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MegaEaterMMO.Tests
{
    public class MegaEaterTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureServices(services =>
            {
                var sp = services.BuildServiceProvider();

                var redis = sp.GetRequiredService<IConnectionMultiplexer>();

                var db = redis.GetDatabase();

                db.ExecuteAsync("FLUSHDB"); // Clear cache 
                // Otherwise rate-limiting WILL bite 
            });
        }
    }
}
