using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebCrawler;

public sealed class ArticlePublisher
{
    public const string HttpClientName = "NewsWebApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<CrawlerOptions> _options;
    private readonly ILogger<ArticlePublisher> _logger;

    public ArticlePublisher(
        IHttpClientFactory httpClientFactory,
        IOptions<CrawlerOptions> options,
        ILogger<ArticlePublisher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<PublishResult> PublishAsync(CrawledArticle article, CancellationToken cancellationToken)
    {
        var options = _options.Value;

        if (options.DryRun)
        {
            _logger.LogInformation(
                "Dry run: would publish '{Headline}' from {SourceUrl} with category '{CategoryId}' and shock value {ShockValue}.",
                article.HeadLine,
                article.SourceUrl,
                article.CategoryId,
                article.ShockValue);

            return new PublishResult(Success: true, ShouldMarkSeen: false);
        }

        var request = new CreateArticleRequest
        {
            HeadLine = article.HeadLine,
            Body = article.Body,
            CategoryId = article.CategoryId,
            AuthorId = options.AuthorId,
            AuthorEmail = options.AuthorEmail,
            SourceUrl = article.SourceUrl,
            ShockValue = article.ShockValue
        };

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync("api/articles/add", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var status = response.StatusCode == HttpStatusCode.OK ? "already existed" : "created";

                _logger.LogInformation(
                    "Article {Status}: '{Headline}' from {SourceUrl} with category '{CategoryId}' and shock value {ShockValue}.",
                    status,
                    article.HeadLine,
                    article.SourceUrl,
                    article.CategoryId,
                    article.ShockValue);

                return new PublishResult(Success: true, ShouldMarkSeen: true);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Could not publish '{Headline}'. API returned {StatusCode}: {Error}",
                article.HeadLine,
                response.StatusCode,
                error);

            return new PublishResult(Success: false, ShouldMarkSeen: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not publish '{Headline}'.", article.HeadLine);
            return new PublishResult(Success: false, ShouldMarkSeen: false);
        }
    }

    public async Task<bool> CanReachApiAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;

        if (options.DryRun)
        {
            return true;
        }

        return await TryReachApiAsync(logFailure: true, cancellationToken);
    }

    public async Task<bool> WaitForApiAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;

        if (options.DryRun)
        {
            return true;
        }

        var deadline = DateTimeOffset.UtcNow.Add(options.EffectiveApiStartupTimeout);
        var attempt = 1;

        while (DateTimeOffset.UtcNow <= deadline)
        {
            if (await TryReachApiAsync(logFailure: false, cancellationToken))
            {
                if (attempt > 1)
                {
                    _logger.LogInformation("News API became reachable at {ApiBaseUrl}.", options.ApiBaseUrl);
                }

                return true;
            }

            if (attempt == 1)
            {
                _logger.LogInformation(
                    "Waiting up to {TimeoutSeconds} seconds for News API at {ApiBaseUrl}.",
                    (int)options.EffectiveApiStartupTimeout.TotalSeconds,
                    options.ApiBaseUrl);
            }

            await Task.Delay(options.EffectiveApiStartupRetryDelay, cancellationToken);
            attempt++;
        }

        _logger.LogWarning(
            "News API did not become reachable at {ApiBaseUrl} within {TimeoutSeconds} seconds.",
            options.ApiBaseUrl,
            (int)options.EffectiveApiStartupTimeout.TotalSeconds);

        return false;
    }

    private async Task<bool> TryReachApiAsync(bool logFailure, CancellationToken cancellationToken)
    {
        var options = _options.Value;

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, string.Empty);
            using var response = await client.SendAsync(request, cancellationToken);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (logFailure)
            {
                _logger.LogWarning(
                    exception,
                    "News API is not reachable at {ApiBaseUrl}. Start NewsWebApi before running the crawler.",
                    options.ApiBaseUrl);
            }

            return false;
        }
    }
}
