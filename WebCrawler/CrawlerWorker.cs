using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebCrawler;

public sealed class CrawlerWorker : BackgroundService
{
    private readonly NewsCrawler _crawler;
    private readonly IArticleAnalyzer _articleAnalyzer;
    private readonly ArticlePublisher _publisher;
    private readonly CrawlHistoryStore _historyStore;
    private readonly IOptions<CrawlerOptions> _options;
    private readonly CrawlerRuntimeOptions _runtimeOptions;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<CrawlerWorker> _logger;

    public CrawlerWorker(
        NewsCrawler crawler,
        IArticleAnalyzer articleAnalyzer,
        ArticlePublisher publisher,
        CrawlHistoryStore historyStore,
        IOptions<CrawlerOptions> options,
        CrawlerRuntimeOptions runtimeOptions,
        IHostApplicationLifetime applicationLifetime,
        ILogger<CrawlerWorker> logger)
    {
        _crawler = crawler;
        _articleAnalyzer = articleAnalyzer;
        _publisher = publisher;
        _historyStore = historyStore;
        _options = options;
        _runtimeOptions = runtimeOptions;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Value.RunOnStartup || _runtimeOptions.RunOnce)
        {
            await RunCrawlerAsync(stoppingToken);
        }

        if (_runtimeOptions.RunOnce)
        {
            _applicationLifetime.StopApplication();
            return;
        }

        using var timer = new PeriodicTimer(_options.Value.EffectiveInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCrawlerAsync(stoppingToken);
        }
    }

    private async Task RunCrawlerAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;

        try
        {
            _logger.LogInformation("Starting crawler run.");
            _logger.LogInformation(
                "Crawler configuration: LLM analysis is {LlmStatus}; provider is {LlmProvider}; model is {LlmModel}; News API base URL is {ApiBaseUrl}.",
                options.Llm.Enabled ? "enabled" : "disabled",
                options.Llm.Provider,
                options.Llm.Model,
                options.ApiBaseUrl);

            if (!await _publisher.WaitForApiAsync(cancellationToken))
            {
                _logger.LogWarning("Crawler run stopped before crawling because the News API is unavailable.");
                return;
            }

            var articles = await _crawler.CrawlAsync(cancellationToken);
            var published = 0;
            var skippedSeen = 0;
            var failed = 0;

            foreach (var article in articles)
            {
                if (await _historyStore.HasSeenAsync(article.SourceUrl, cancellationToken))
                {
                    skippedSeen++;
                    continue;
                }

                var analyzedArticle = await _articleAnalyzer.AnalyzeAsync(article, cancellationToken);
                var result = await _publisher.PublishAsync(analyzedArticle, cancellationToken);

                if (result.Success)
                {
                    if (result.ShouldMarkSeen)
                    {
                        await _historyStore.MarkSeenAsync(analyzedArticle.SourceUrl, analyzedArticle.HeadLine, cancellationToken);
                    }

                    if (!options.DryRun)
                    {
                        published++;
                    }

                    continue;
                }

                failed++;
            }

            _logger.LogInformation(
                "Crawler run finished. Extracted: {Extracted}. Published: {Published}. Skipped seen: {SkippedSeen}. Failed: {Failed}.",
                articles.Count,
                published,
                skippedSeen,
                failed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Crawler run failed.");
        }
    }
}
