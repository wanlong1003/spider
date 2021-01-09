using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
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
    /// <summary>
    /// BT之家数据爬取
    /// http://www.415.net/
    /// </summary>
    public class BTHome
    {
        private IHttpClientFactory _httpClientFactory;
        private ILogger _logger;

        public BTHome(IHttpClientFactory httpClientFactory, ILogger<BTHome> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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
                    var tasks = new List<Task>();
                    foreach (var item in list)
                    {
                        try
                        {
                            var task = DownLoadTorrent(item.Title, item.Url, cancellationTokenSource.Token);
                            tasks.Add(task);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                    Task.WaitAll(tasks.ToArray(), cancellationTokenSource.Token);
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

        public async Task<IEnumerable<(string Title, string Url)>> CrawlList(int pageIndex, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"下载列表页：{pageIndex}");
            //var url = $"http://www.3btjia.com/forum-index-fid-1-page-{pageIndex}.htm";  //电影
            //var url = $"http://www.3btjia.com/forum-index-fid-9-page-{pageIndex}.htm"; //福利
            var url = $"http://www.3btjia.com/forum-index-fid-8-page-{pageIndex}.htm"; //图片
            var httpClient = CreateClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = new HtmlDocument();
            doc.Load(stream, Encoding.UTF8);
            var nodes = doc.DocumentNode.SelectNodes("//div[@id='threadlist']//a")?.Where(d => d.HasClass("subject_link"));
            var list = new List<(string Title, string Url)>();
            if (nodes != null && nodes.Count() > 0)
            {
                foreach (var node in nodes)
                {
                    _logger.LogInformation($"{node.InnerText} : {node.GetAttributeValue("href", "")}");
                    list.Add((node.InnerText, node.GetAttributeValue("href", "")));
                }
            }
            _logger.LogInformation($"下载列表页成功：{pageIndex}, 共计{list.Count}条");
            return list;
        }

        public async Task DownLoadTorrent(string name, string url, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"下载文件：{name}, 地址：{url}");
            var httpClient = CreateClient();
            var responsePage = await httpClient.GetAsync(url, cancellationToken);
            var stream = await responsePage.Content.ReadAsStreamAsync();
            var doc = new HtmlDocument();
            doc.Load(stream, Encoding.UTF8);
            var files = doc.DocumentNode.SelectNodes("//div[@class='attachlist']//a[@class='ajaxdialog']");
            if (files != null && files.Count > 0)
            {
                for (var i = 0; i < files.Count; i++)
                {
                    try
                    {
                        var fileUrl = files[i].GetAttributeValue("href", "");
                        fileUrl = fileUrl.Replace("dialog", "download").Replace("-ajax-1", "");
                        _logger.LogInformation($"下载地址: {fileUrl}");
                        var downloadClient = CreateClient();
                        var response = await downloadClient.GetAsync(fileUrl, cancellationToken);
                        var arr = await response.Content.ReadAsByteArrayAsync();
                        using var file = new FileStream($"download/{ReplaceBadCharOfFileName(name)}.torrent", FileMode.Create);
                        file.Write(arr);
                        file.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                }
                _logger.LogInformation($"下载文件成功：{name}, 地址：{url}");
            }
            else
            {
                _logger.LogInformation($"没有找到文件地址：{name}, 地址：{url}");
            }
        }


        private string ReplaceBadCharOfFileName(string fileName)
        {
            string str = fileName;
            str = str.Replace("\\", string.Empty);
            str = str.Replace("/", string.Empty);
            str = str.Replace(":", string.Empty);
            str = str.Replace("*", string.Empty);
            str = str.Replace("?", string.Empty);
            str = str.Replace("\"", string.Empty);
            str = str.Replace("<", string.Empty);
            str = str.Replace(">", string.Empty);
            str = str.Replace("|", string.Empty);
            str = str.Replace(" ", string.Empty);    //前面的替换会产生空格,最后将其一并替换掉
            return str;
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
