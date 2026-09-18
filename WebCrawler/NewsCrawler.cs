using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebCrawler;

public sealed class NewsCrawler
{
    public const string HttpClientName = "NewsPages";

    private static readonly string[] ExcludedPathParts =
    [
        "/wp-content/",
        "/wp-json/",
        "/feed",
        "/comments",
        "/newsletter",
        "/donacii",
        "/nekrolozi",
        "/marketing",
        "/contact",
        "/privacy",
        "/sq/",
        "/sr/",
        "/tr/",
        "/bg/",
        "/el/",
        "/en/",
        "/de/",
        "/author/",
        "/tag/",
        "/search",
        "/rss",
        "/widget",
        "/info/",
        "/personal",
        "/arhiva",
        "/trezor",
        "/javni"
    ];

    private static readonly string[] ExcludedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp",
        ".avif",
        ".svg",
        ".pdf",
        ".zip",
        ".mp3",
        ".mp4"
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ArticleExtractor _articleExtractor;
    private readonly IOptions<CrawlerOptions> _options;
    private readonly ILogger<NewsCrawler> _logger;

    /// <summary>Constructs the crawler with its fetch, extraction, configuration and logging dependencies.</summary>
    public NewsCrawler(
        IHttpClientFactory httpClientFactory,
        ArticleExtractor articleExtractor,
        IOptions<CrawlerOptions> options,
        ILogger<NewsCrawler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _articleExtractor = articleExtractor;
        _options = options;
        _logger = logger;
    }

    /// <summary>Discovers headline candidates and downloads only eligible articles.</summary>
    public async Task<IReadOnlyList<CrawledArticle>> CrawlAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var seedUris = options.SeedUrls
            .Select(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null)
            .OfType<Uri>()
            .ToList();

        if (seedUris.Count == 0)
        {
            _logger.LogWarning("No valid crawler seed URLs are configured.");
            return [];
        }

        var seedHosts = seedUris
            .Select(NormalizeHost)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var discoveryQueue = new Queue<(Uri Uri, int Depth)>();
        foreach (var seedUri in seedUris)
        {
            discoveryQueue.Enqueue((NormalizeUri(seedUri), 0));
        }

        var visitedDiscoveryPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new Dictionary<Uri, List<string>>();
        var headlineFilter = new HeadlineFilter(options.HeadlineFilter);
        var articles = new List<CrawledArticle>();

        while (discoveryQueue.Count > 0 &&
               visitedDiscoveryPages.Count < Math.Max(1, options.MaxDiscoveryPagesPerRun))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (uri, depth) = discoveryQueue.Dequeue();
            var normalizedUri = NormalizeUri(uri);

            if (!visitedDiscoveryPages.Add(normalizedUri.AbsoluteUri))
            {
                continue;
            }

            var page = await FetchPageAsync(normalizedUri, cancellationToken);
            if (page is null || !IsHtml(page.ContentType))
            {
                continue;
            }

            var document = new HtmlDocument();
            document.LoadHtml(page.Html);

            foreach (var (link, headlines) in ExtractLinks(document, page.FinalUri))
            {
                if (!IsDiscoverable(link, seedHosts))
                {
                    continue;
                }

                if (IsPotentialArticleUrl(link))
                {
                    if (!candidates.TryGetValue(link, out var existingHeadlines))
                    {
                        existingHeadlines = [];
                        candidates.Add(link, existingHeadlines);
                    }

                    existingHeadlines.AddRange(headlines);

                    continue;
                }

                if (depth < 1 && IsListingUrl(link))
                {
                    discoveryQueue.Enqueue((link, depth + 1));
                }
            }

            await DelayAsync(options, cancellationToken);
        }

        // Filter before the run limit so rejected links cannot crowd out eligible articles.
        var candidateUrls = new List<Uri>();
        foreach (var (uri, headlines) in candidates)
        {
            var decision = headlineFilter.Evaluate(headlines);
            if (decision == HeadlineDecision.Accepted)
            {
                candidateUrls.Add(uri);
            }
            else
            {
                _logger.LogDebug("Skipping {Url}: headline filter returned {Decision}.", uri, decision);
            }
        }

        _logger.LogInformation("Headline filter accepted {Accepted} of {Discovered} discovered article URLs.",
            candidateUrls.Count, candidates.Count);

        foreach (var candidateUri in SelectArticleCandidates(candidateUrls, Math.Max(1, options.MaxArticlesPerRun)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await FetchPageAsync(candidateUri, cancellationToken);
            if (page is null || !IsHtml(page.ContentType))
            {
                continue;
            }

            var article = _articleExtractor.Extract(page);
            if (article is not null)
            {
                articles.Add(article);
            }

            await DelayAsync(options, cancellationToken);
        }

        return articles;
    }

    /// <summary>Selects eligible URLs round-robin across sources within the run limit.</summary>
    private static IReadOnlyList<Uri> SelectArticleCandidates(IReadOnlyList<Uri> candidates, int maxArticles)
    {
        var groupedCandidates = candidates
            .GroupBy(NormalizeHost)
            .Select(group => new Queue<Uri>(group))
            .ToList();

        var selectedCandidates = new List<Uri>();

        while (selectedCandidates.Count < maxArticles && groupedCandidates.Any(group => group.Count > 0))
        {
            foreach (var group in groupedCandidates)
            {
                if (group.Count == 0)
                {
                    continue;
                }

                selectedCandidates.Add(group.Dequeue());

                if (selectedCandidates.Count >= maxArticles)
                {
                    break;
                }
            }
        }

        return selectedCandidates;
    }

    /// <summary>Fetches a page while preserving cancellation and logging network failures.</summary>
    private async Task<FetchedPage?> FetchPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            if (NormalizeHost(uri).Equals("time.mk", StringComparison.OrdinalIgnoreCase) &&
                uri.AbsolutePath.StartsWith("/r/", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Referrer = new Uri("https://time.mk/");
            }

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Skipping {Url}; status code was {StatusCode}.", uri, response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var finalUri = response.RequestMessage?.RequestUri ?? uri;

            return new FetchedPage(uri, NormalizeUri(finalUri), html, contentType);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not fetch {Url}.", uri);
            return null;
        }
    }

    /// <summary>Extracts URLs and available headline labels without requesting article pages.</summary>
    private static IEnumerable<(Uri Uri, IReadOnlyList<string> Headlines)> ExtractLinks(HtmlDocument document, Uri baseUri)
    {
        var links = document.DocumentNode.SelectNodes("//a[@href]");
        if (links is null)
        {
            yield break;
        }

        foreach (var link in links)
        {
            var href = HtmlEntity.DeEntitize(link.GetAttributeValue("href", string.Empty)).Trim();

            if (string.IsNullOrWhiteSpace(href) ||
                href.StartsWith('#') ||
                href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!Uri.TryCreate(baseUri, href, out var uri))
            {
                continue;
            }

            // Keep all labels: a harmless title attribute must not hide a serious visible headline.
            string[] headlines =
            [
                link.InnerText,
                link.GetAttributeValue("title", string.Empty),
                link.GetAttributeValue("aria-label", string.Empty),
                link.SelectSingleNode(".//img[@alt]")?.GetAttributeValue("alt", string.Empty) ?? string.Empty
            ];
            yield return (NormalizeUri(uri), headlines);
        }
    }

    /// <summary>Restricts discovery to configured sources and supported paths.</summary>
    private static bool IsDiscoverable(Uri uri, HashSet<string> seedHosts)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (!seedHosts.Contains(NormalizeHost(uri)))
        {
            return false;
        }

        var path = uri.AbsolutePath.ToLowerInvariant();

        if (ExcludedPathParts.Any(path.Contains) ||
            ExcludedExtensions.Any(path.EndsWith))
        {
            return false;
        }

        return IsPotentialArticleUrl(uri) || IsListingUrl(uri);
    }

    /// <summary>Identifies article-shaped URLs using the existing source routing conventions.</summary>
    private static bool IsPotentialArticleUrl(Uri uri)
    {
        var host = NormalizeHost(uri);
        var path = uri.AbsolutePath.Trim('/').ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (host.Equals("time.mk", StringComparison.OrdinalIgnoreCase))
        {
            return path.StartsWith("r/", StringComparison.OrdinalIgnoreCase);
        }

        if (path.StartsWith("category/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("author/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("tag/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("page/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("najnovo", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 1 && segments[0].Length <= 3)
        {
            return false;
        }

        return segments.Length <= 2;
    }

    /// <summary>Identifies home and category pages used for headline discovery.</summary>
    private static bool IsListingUrl(Uri uri)
    {
        var host = NormalizeHost(uri);
        var path = uri.AbsolutePath.Trim('/').ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        if (path.StartsWith("category/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("najnovo", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (host.Equals("time.mk", StringComparison.OrdinalIgnoreCase))
        {
            return path.StartsWith("st/", StringComparison.OrdinalIgnoreCase) ||
                   path.Equals("n/all", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>Determines whether a response can be parsed as markup.</summary>
    private static bool IsHtml(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType) ||
               contentType.Contains("html", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("xml", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Respects the configured delay between source requests.</summary>
    private static async Task DelayAsync(CrawlerOptions options, CancellationToken cancellationToken)
    {
        if (options.RequestDelayMilliseconds > 0)
        {
            await Task.Delay(options.RequestDelayMilliseconds, cancellationToken);
        }
    }

    /// <summary>Removes fragments and default ports for URL deduplication.</summary>
    private static Uri NormalizeUri(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };

        if ((builder.Scheme == Uri.UriSchemeHttp && builder.Port == 80) ||
            (builder.Scheme == Uri.UriSchemeHttps && builder.Port == 443))
        {
            builder.Port = -1;
        }

        return builder.Uri;
    }

    /// <summary>Treats www and bare source hosts as the same source.</summary>
    private static string NormalizeHost(Uri uri)
    {
        return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..]
            : uri.Host;
    }
}
