using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spider.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spider
{
    public class YinFans
    {
        private IHttpClientFactory _httpClientFactory;
        private ILogger _logger;
        private DefaultDbContext _dbContext;

        public YinFans(IHttpClientFactory httpClientFactory, ILogger<YinFans> logger, DefaultDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _dbContext = dbContext;
            _dbContext.Database.Migrate();
        }

        public async Task<IEnumerable<(string Title, string Url)>> CrawlList(int pageIndex, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"下载列表页：{pageIndex}");
            var url = $"http://www.yinfans.me/page/{pageIndex}";
            var httpClient = CreateClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = new HtmlDocument();
            doc.Load(stream, Encoding.UTF8);
            var nodes = doc.DocumentNode.SelectNodes("//a")?.Where(d => d.HasClass("zoom"));
            var list = new List<(string Title, string Url)>();
            if (nodes != null && nodes.Count() > 0)
            {
                foreach (var node in nodes)
                {
                    var title = node.GetAttributeValue("title", "");
                    var href = node.GetAttributeValue("href", "");
                    _logger.LogInformation($"{ title}: {href}");
                    list.Add((title, href));
                }
            }
            _logger.LogInformation($"下载列表页成功：{pageIndex}, 共计{list.Count}条");
            return list;
        }

        public async Task CrawlMagnet(string url, CancellationToken cancellationToken)
        {
            var httpClient = CreateClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            var stream = await response.Content.ReadAsStreamAsync();
            stream.Position = 0;
            var html = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//*[@id='content']/div[1]/h1")?.InnerText;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = doc.DocumentNode.SelectSingleNode("/html/head/title").InnerText;
            }
            var movieEntity = new MovieEntity()
            {
                Id = Guid.NewGuid(),
                Name = title,
                Content = doc.DocumentNode.SelectSingleNode("//*[@id='post_content']")?.InnerHtml,
                Source = url
            };

            var nodes = doc.DocumentNode.SelectNodes("//table[@id='cili']//a");
            var list = new List<DownLoadEntity>();
            if (nodes != null && nodes.Count() > 0)
            {
                foreach (var node in nodes)
                {
                    var name = node.SelectSingleNode("b")?.InnerText;
                    var size = node.SelectSingleNode("span/span[@class='label label-warning']")?.InnerText;
                    var quality = node.SelectSingleNode("span/span[@class='label label-danger']")?.InnerText;
                    var magnet = node.GetAttributeValue("href", "");
                    list.Add(new DownLoadEntity { Movie = movieEntity.Id, Name = name, Size = size, Quality = quality, Address = magnet });
                    _logger.LogInformation($"{quality} : {size} : { name} : {magnet}");
                }
            }
            _dbContext.Set<MovieEntity>().Add(movieEntity);
            _dbContext.Set<DownLoadEntity>().AddRange(list);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Start(int startPage, int endPage)
        {
            var stopWatch = new Stopwatch();
            for (var i = startPage; i <= endPage; i++)
            {
                stopWatch.Restart();
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(300000);
                try
                {
                    var list = await CrawlList(i, cancellationTokenSource.Token);
                    foreach (var item in list)
                    {
                        try
                        {
                            await CrawlMagnet(item.Url, cancellationTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                _logger.LogInformation($"第{i}页耗时: {stopWatch.Elapsed.TotalSeconds} 秒");
                await Task.Delay(5000);
            }
            stopWatch.Stop();
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(300);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36 Edg/84.0.522.50");
            client.DefaultRequestHeaders.Add("Cookie", "bbs_sid=c35f9b14e719b805; bbs_lastday=1596328440; bbs_lastonlineupdate=1596328473");
            return client;
        }
    }
}
