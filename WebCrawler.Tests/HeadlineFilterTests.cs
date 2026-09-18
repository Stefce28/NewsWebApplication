using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace WebCrawler.Tests;

public sealed class HeadlineFilterTests
{
    /// <summary>Checks normalization, boundaries, missing titles and exclusion precedence.</summary>
    [Theory]
    [InlineData("ШОКАНТНО: мачка во воз", HeadlineDecision.Accepted)]
    [InlineData("&#1096;окантно: мачка", HeadlineDecision.Accepted)]
    [InlineData("нешокантно", HeadlineDecision.NoComedyKeyword)]
    [InlineData("Обична вест", HeadlineDecision.NoComedyKeyword)]
    [InlineData(" ", HeadlineDecision.MissingHeadline)]
    [InlineData("Шокантно: загинати војници на фронтот", HeadlineDecision.SensitiveTopic)]
    [InlineData("Драматично: протестите во Иран за слобода на говор", HeadlineDecision.SensitiveTopic)]
    public void EvaluatesHeadline(string headline, HeadlineDecision expected)
    {
        Assert.Equal(expected, new HeadlineFilter(new()).Evaluate([headline]));
    }

    /// <summary>Ensures every requested comedy keyword is enabled by default.</summary>
    [Fact]
    public void AcceptsDefaultComedyKeywords()
    {
        var options = new HeadlineFilterOptions();
        var filter = new HeadlineFilter(options);
        Assert.Equal(19, options.ComedyKeywords.Length);
        foreach (var keyword in options.ComedyKeywords)
            Assert.Equal(HeadlineDecision.Accepted, filter.Evaluate([$"{keyword}: мачка во воз"]));
    }

    /// <summary>Verifies configurable rules and the explicit legacy behavior switch.</summary>
    [Fact]
    public void SupportsCustomRulesAndDisabling()
    {
        var options = new HeadlineFilterOptions { ComedyKeywords = ["мачка"], SensitiveKeywords = ["опасно"] };
        Assert.Equal(HeadlineDecision.Accepted, new HeadlineFilter(options).Evaluate(["мачка"]));
        Assert.Equal(HeadlineDecision.SensitiveTopic, new HeadlineFilter(options).Evaluate(["опасно", "мачка"]));
        options.Enabled = false;
        Assert.Equal(HeadlineDecision.Accepted, new HeadlineFilter(options).Evaluate([]));
    }

    /// <summary>Proves rejected and duplicate links never cause article HTTP requests or consume the limit.</summary>
    [Fact]
    public async Task DownloadsOnlyEligibleArticles()
    {
        using var handler = new ListingHandler();
        using var client = new HttpClient(handler);
        var options = Options.Create(new CrawlerOptions
        {
            SeedUrls = ["https://example.test/"], MaxArticlesPerRun = 1,
            RequestDelayMilliseconds = 0, MinArticleBodyCharacters = 40
        });
        var crawler = new NewsCrawler(new ClientFactory(client), new ArticleExtractor(options), options,
            NullLogger<NewsCrawler>.Instance);

        var articles = await crawler.CrawlAsync(CancellationToken.None);

        Assert.Single(articles);
        Assert.Equal(new[] { "/", "/funny-story" }, handler.Paths);
        Assert.Equal("Шокантно: мачка во воз", articles[0].HeadLine);
    }

    /// <summary>Rejects source titles that reveal sensitive content hidden by listing labels.</summary>
    [Fact]
    public void RechecksSourceHeadline()
    {
        var extractor = new ArticleExtractor(Options.Create(new CrawlerOptions()));
        var uri = new Uri("https://example.test/story");
        Assert.Null(extractor.Extract(new FetchedPage(uri, uri,
            "<h1>Шокантно: загинати војници</h1><article><p>" + new string('а', 400) + "</p></article>", "text/html")));
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        /// <summary>Supplies the offline HTTP client for crawler requests.</summary>
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class ListingHandler : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];

        /// <summary>Serves fixtures and records all downloads without accessing live sources.</summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            var html = request.RequestUri.AbsolutePath == "/" ? """
                <a href="/ordinary-story">Обична вест</a>
                <a href="/serious-story">Шокантно: загинати војници</a>
                <a href="/missing-title"><img src="photo.jpg"></a>
                <a href="/duplicate-story">Шокантно: мачка</a>
                <a href="/duplicate-story">Загинати војници</a>
                <a href="/funny-story"><img alt="Шокантно: мачка во воз"></a>
                <a href="/funny-story">Шокантно: мачка во воз</a>
                """ : "<h1>Шокантно: мачка во воз</h1><article><p>" + new string('а', 400) + "</p></article>";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request, Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
            });
        }
    }
}
