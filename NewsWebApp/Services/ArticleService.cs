using NewsWebApp.Models;
using NewsWebApp.Repository;
using NewsWebApp.Services.IServices;
using NewsWebApp.ViewModels;

namespace NewsWebApp.Services;

public class ArticleService : IArticleService
{
    private readonly IArticleRepository _articleRepository;
    private readonly ICategoryService _categoryService;
    private readonly IUserService _userService;
    
    
    public ArticleService(IArticleRepository repository,  ICategoryService categoryService, IUserService userService)
    {
        this._articleRepository = repository;
        this._categoryService = categoryService;
        this._userService = userService;
    }


    public async Task<Article?> GetArticleById(Guid id)
    {
        return await _articleRepository.GetArticle(id);
    }

    public async Task<List<Article>> GetArticles()
    {
        return await _articleRepository.GetArticles();
    }

    public async Task<ArticleListViewModel> GetArticleList(int page, int pageSize, string? categoryName = null, Guid? authorId = null, string? title = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Max(pageSize, 1);

        int totalArticles;
        List<Article> articles;

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            totalArticles = await _articleRepository.CountArticlesByCategory(categoryName);
            var totalPagesForCategory = Math.Max(1, (int)Math.Ceiling(totalArticles / (double)pageSize));
            page = Math.Min(page, totalPagesForCategory);
            articles = await _articleRepository.GetArticlesByCategoryPage(categoryName, page, pageSize);
        }
        else if (authorId.HasValue)
        {
            totalArticles = await _articleRepository.CountArticlesByAuthor(authorId.Value);
            var totalPagesForAuthor = Math.Max(1, (int)Math.Ceiling(totalArticles / (double)pageSize));
            page = Math.Min(page, totalPagesForAuthor);
            articles = await _articleRepository.GetArticlesByAuthorPage(authorId.Value, page, pageSize);
        }
        else
        {
            totalArticles = await _articleRepository.CountArticles();
            var totalPagesForAllArticles = Math.Max(1, (int)Math.Ceiling(totalArticles / (double)pageSize));
            page = Math.Min(page, totalPagesForAllArticles);
            articles = await _articleRepository.GetArticlesPage(page, pageSize);
        }

        return new ArticleListViewModel
        {
            Articles = articles,
            CurrentPage = page,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalArticles / (double)pageSize)),
            CategoryName = categoryName,
            AuthorId = authorId,
            Title = title ?? "Најнови вести"
        };
    }

    public async Task<List<Article>?> GetArticlesByCategory(string categoryName)
    {
        var articles = await GetArticles();
        
        return articles.Where(a => a.Category.Name == categoryName)
                .Select(a => a).ToList();
       
    }

    public async Task<List<Article>?> GetArticlesByAuthor(Guid authorId)
    {
        var articles = await GetArticles();
        return articles.Where(a => a.Author.Id == authorId).ToList();

    }

    public async Task<Article?> AddArticleAsync(String title, String content, Category? category,User? author)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content) || category == null || author == null)
            return null;

        Article article = new Article(title, content, category, author);
        await _articleRepository.SaveArticle(article);
        return article;
    }

    public async Task<bool> DeleteArticleAsync(Guid id)
    {
        var a = await GetArticleById(id);
        if (a == null)
            return false;
        await _articleRepository.DeleteArticle(a);
        return true;
    }

    public async Task<Article?> UpdateArticle(Guid id, string title, string content, Category category, User author)
    {
        var a = await GetArticleById(id);
        if (a == null)
            return null;
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return null;
        a.HeadLine = title;
        a.Body= content;
        a.Category = category;
        a.Author = author;
        await _articleRepository.UpdateArticle(a);
        return a;
    }
}
