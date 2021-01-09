using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spider.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Spider
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var sereviceProvider = new ServiceCollection()
                .AddHttpClient()
                .AddLogging(builder => { builder.AddConsole(); })
                .AddDbContext<DefaultDbContext>(options=> {
                    options.UseSqlite($"Data Source=mydb_{DateTime.Now.ToString("yyyyMMddHHmmss")}.db");
                })
                .AddTransient<BTHome>()
                .AddTransient<YinFans>()
                .BuildServiceProvider();

            using var scope = sereviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            //var bt = scope.ServiceProvider.GetRequiredService<BTHome>();
            //await bt.Start(1, 52);
            var yinFans = scope.ServiceProvider.GetRequiredService<YinFans>();
            //var pages = await yinFans.CrawlList(1, new CancellationTokenSource().Token);
            await yinFans.Start(1, 102);
            logger.LogInformation($"获取结束");
            Console.ReadLine();
        }
    }
}
