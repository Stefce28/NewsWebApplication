using NewsWebApp.Models;
using NewsWebApp.ViewModels;

namespace NewsWebApp.Services.IServices;

public interface IArticleService
{
    Task <Article?> GetArticleById(Guid id);
    Task<ArticleListViewModel> GetArticleList(int page, int pageSize, string? categoryName = null, Guid? authorId = null, string? title = null);
    Task<List<Article>?> GetArticlesByCategory(String categoryName);
    Task<List<Article>?> GetArticlesByAuthor(Guid authorId);
    Task<Article?> AddArticleAsync(String title, String content, Category category,User author);
    Task<bool> DeleteArticleAsync(Guid id);
    Task<Article?> UpdateArticle(Guid id, String title, String content, Category category, User author);
}
