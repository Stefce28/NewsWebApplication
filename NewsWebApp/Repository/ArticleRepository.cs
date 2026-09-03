using Microsoft.EntityFrameworkCore;
using NewsWebApp.Data;
using NewsWebApp.Models;

namespace NewsWebApp.Repository;

public class ArticleRepository : IArticleRepository
{
    private readonly ApplicationDbContext _context;
    
    public ArticleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<Article> ArticlesWithRelations()
    {
        return _context.Articles
            .Include(a => a.Category)
            .Include(a => a.Author);
    }
    
    public async Task<List<Article>> GetArticles()
    {
        return await ArticlesWithRelations()
            .OrderBy(a => a.HeadLine)
            .ToListAsync();
    }

    public async Task<Article?> GetArticle(Guid id)
    {
        return await ArticlesWithRelations()
            .FirstOrDefaultAsync(a => a.Pid == id);
    }

    public async Task<List<Article>> GetArticlesPage(int page, int pageSize)
    {
        return await ArticlesWithRelations()
            .OrderBy(a => a.HeadLine)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Article>> GetArticlesByCategoryPage(string categoryName, int page, int pageSize)
    {
        return await ArticlesWithRelations()
            .Where(a => EF.Property<string>(a, "CategoryName") == categoryName)
            .OrderBy(a => a.HeadLine)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Article>> GetArticlesByAuthorPage(Guid authorId, int page, int pageSize)
    {
        return await ArticlesWithRelations()
            .Where(a => EF.Property<Guid>(a, "AuthorId") == authorId)
            .OrderBy(a => a.HeadLine)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountArticles()
    {
        return await _context.Articles.CountAsync();
    }

    public async Task<int> CountArticlesByCategory(string categoryName)
    {
        return await _context.Articles
            .CountAsync(a => EF.Property<string>(a, "CategoryName") == categoryName);
    }

    public async Task<int> CountArticlesByAuthor(Guid authorId)
    {
        return await _context.Articles
            .CountAsync(a => EF.Property<Guid>(a, "AuthorId") == authorId);
    }

    public async Task SaveArticle(Article article)
    {
        await _context.Articles.AddAsync(article);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateArticle(Article article)
    {
        _context.Articles.Update(article);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteArticle(Article article)
    {
        await _context.Articles
            .Where(a => a.Pid == article.Pid)
            .ExecuteDeleteAsync();
    }
}
