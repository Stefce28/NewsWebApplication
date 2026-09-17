using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WebCrawler;

public sealed class CrawlHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IOptions<CrawlerOptions> _options;
    private CrawlerHistory? _history;

    public CrawlHistoryStore(IOptions<CrawlerOptions> options)
    {
        _options = options;
    }

    public async Task<bool> HasSeenAsync(string sourceUrl, CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);

        return _history!.SeenArticles.ContainsKey(NormalizeUrl(sourceUrl));
    }

    public async Task MarkSeenAsync(string sourceUrl, string headline, CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);

        try
        {
            _history!.SeenArticles[NormalizeUrl(sourceUrl)] = new SeenArticle
            {
                Headline = headline,
                FirstSeenAtUtc = DateTimeOffset.UtcNow
            };

            await SaveAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_history is not null)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_history is not null)
            {
                return;
            }

            var path = GetHistoryPath();
            if (!File.Exists(path))
            {
                _history = new CrawlerHistory();
                return;
            }

            await using var stream = File.OpenRead(path);
            _history = await JsonSerializer.DeserializeAsync<CrawlerHistory>(
                stream,
                JsonOptions,
                cancellationToken) ?? new CrawlerHistory();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var path = GetHistoryPath();
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, _history, JsonOptions, cancellationToken);
    }

    private string GetHistoryPath()
    {
        return Path.GetFullPath(_options.Value.HistoryFilePath);
    }

    private static string NormalizeUrl(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return sourceUrl.Trim();
        }

        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };

        if ((builder.Scheme == Uri.UriSchemeHttp && builder.Port == 80) ||
            (builder.Scheme == Uri.UriSchemeHttps && builder.Port == 443))
        {
            builder.Port = -1;
        }

        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }

    private sealed class CrawlerHistory
    {
        public Dictionary<string, SeenArticle> SeenArticles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SeenArticle
    {
        public string Headline { get; set; } = string.Empty;
        public DateTimeOffset FirstSeenAtUtc { get; set; }
    }
}
