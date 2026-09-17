namespace WebCrawler;

public sealed class PassthroughArticleAnalyzer : IArticleAnalyzer
{
    public Task<CrawledArticle> AnalyzeAsync(
        CrawledArticle article,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(article);
    }
}
