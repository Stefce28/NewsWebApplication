namespace WebCrawler;

public sealed record FetchedPage(
    Uri RequestedUri,
    Uri FinalUri,
    string Html,
    string? ContentType);

public sealed record CrawledArticle(
    string SourceUrl,
    string HeadLine,
    string Body,
    string CategoryId,
    decimal ShockValue);

public sealed record PublishResult(bool Success, bool ShouldMarkSeen);

public sealed class CreateArticleRequest
{
    public string HeadLine { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public Guid? AuthorId { get; set; }
    public string? AuthorEmail { get; set; }
    public string? SourceUrl { get; set; }
    public decimal ShockValue { get; set; }
}
