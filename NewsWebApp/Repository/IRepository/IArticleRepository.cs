using NewsWebApp.Shared.Models;

namespace NewsWebApp.Repository;

public interface IArticleRepository
{
    Task<List<Article>> GetArticles();
    Task<Article?> GetArticle(Guid id);
    Task<List<Article>> GetArticlesPage(int page, int pageSize);
    Task<List<Article>> GetArticlesByCategoryPage(string categoryName, int page, int pageSize);
    Task<List<Article>> GetArticlesByAuthorPage(Guid authorId, int page, int pageSize);
    Task<int> CountArticles();
    Task<int> CountArticlesByCategory(string categoryName);
    Task<int> CountArticlesByAuthor(Guid authorId);
    Task SaveArticle(Article article);
    Task UpdateArticle(Article article);
    Task DeleteArticle(Article article);
}
