using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Abot2.Crawler;
using Abot2.Poco;

namespace AzureSearchCrawler
{
    /// <summary>
    ///  A convenience wrapper for an Abot crawler with a reasonable default configuration and console logging.
    ///  The actual action to be performed on the crawled pages is passed in as a CrawlHandler.
    /// </summary>
    class Crawler
    {
        private static int PageCount = 0;

        private CrawlHandler _handler;

        public Crawler(CrawlHandler handler)
        {
            _handler = handler;
        }

        public async Task Crawl(string rootUri, int maxPages)
        {
            var crawlConfig = new CrawlConfiguration
            {
                MaxPagesToCrawl = maxPages,
            };
            PoliteWebCrawler crawler = new PoliteWebCrawler(crawlConfig, null, null, null, null, null, null, null, null);

            crawler.PageCrawlStarting += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompleted += crawler_ProcessPageCrawlCompleted;

            CrawlResult result = await crawler.CrawlAsync(new Uri(rootUri)); //This is synchronous, it will not go to the next line until the crawl has completed
            if (result.ErrorOccurred)
            {
                Console.WriteLine("Crawl of {0} ({1} pages) completed with error: {2}", result.RootUri.AbsoluteUri, PageCount, result.ErrorException.Message);
            }
            else
            {
                Console.WriteLine("Crawl of {0} ({1} pages) completed without error.", result.RootUri.AbsoluteUri, PageCount);
            }

            await _handler.CrawlFinishedAsync();
        }

        void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            Interlocked.Increment(ref PageCount);

            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("{0}  found on  {1}", pageToCrawl.Uri.AbsoluteUri, pageToCrawl.ParentUri.AbsoluteUri);
        }

        async void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            string uri = crawledPage.Uri.AbsoluteUri;

            if (crawledPage.HttpRequestException != null || crawledPage.HttpResponseMessage?.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Crawl of page failed {0}: exception '{1}', response status {2}", uri, crawledPage.HttpRequestException?.Message, crawledPage.HttpResponseMessage?.StatusCode);
                return;
            }

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
            {
                Console.WriteLine("Page had no content {0}", uri);
                return;
            }

            await _handler.PageCrawledAsync(crawledPage);
        }

        private CrawlConfiguration CreateCrawlConfiguration(int maxPages)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            CrawlConfiguration crawlConfig = new CrawlConfiguration();
            crawlConfig.CrawlTimeoutSeconds = maxPages * 10;
            crawlConfig.MaxConcurrentThreads = 5;
            crawlConfig.MinCrawlDelayPerDomainMilliSeconds = 100;
            crawlConfig.IsSslCertificateValidationEnabled = true;

            crawlConfig.MaxPagesToCrawl = maxPages;

            return crawlConfig;
        }
    }
}
