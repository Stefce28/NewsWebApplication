namespace WebCrawler;

public interface IArticleAnalyzer
{
    Task<CrawledArticle> AnalyzeAsync(
        CrawledArticle article,
        CancellationToken cancellationToken);
}
