using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
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
                .AddTransient<BTHome>()
                .BuildServiceProvider();

            using var scope = sereviceProvider.CreateScope();
            var bt = scope.ServiceProvider.GetRequiredService<BTHome>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var stopWatch = new Stopwatch();
            for (var i = 34; i <= 52; i++)
            {
                stopWatch.Restart();
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(300000);
                try
                {
                    var list = await bt.CrawlList(i, cancellationTokenSource.Token);
                    var tasks = new List<Task>();
                    foreach (var item in list)
                    {
                        try
                        {
                            var task = bt.DownLoadTorrent(item.Title, item.Url, cancellationTokenSource.Token);
                            tasks.Add(task);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                        }
                    }
                    Task.WaitAll(tasks.ToArray(), cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
                logger.LogInformation($"第{i}页耗时: {stopWatch.Elapsed.TotalSeconds} 秒");
                await Task.Delay(5000);
            }
            stopWatch.Stop();
            Console.WriteLine("获取结束");
            Console.ReadLine();
        }
    }
}
