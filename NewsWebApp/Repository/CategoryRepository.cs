using Microsoft.EntityFrameworkCore;
using NewsWebApp.Data;
using NewsWebApp.Shared.Models;
namespace NewsWebApp.Repository;

public class CategoryRepository : ICategoryRepository
{
    
    private readonly ApplicationDbContext _context;
    public CategoryRepository(ApplicationDbContext dbContext)
    {
        this._context = dbContext;
    }
    
    public async Task<List<Category>> ListAllCategories()
    {
        return await _context.Categories.ToListAsync();
    }

    public async Task<Category?> FindCategoryByName(string categoryName)
    {
        return await _context.Categories.FindAsync(categoryName);
    }
    
    public async Task SaveCategory(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategory(Category category)
    {
        await _context
            .Categories
            .Where(c => c == category)
            .ExecuteDeleteAsync();
    }
    
}