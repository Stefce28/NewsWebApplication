using NewsWebApp.Shared.Models;
using NewsWebApp.Repository;

namespace NewsWebApp.Services.IServices;

public class CategoryService : ICategoryService
{
    
    private readonly ICategoryRepository repository;

    public CategoryService(ICategoryRepository repository)
    {
        this.repository = repository;
    }
        
    
    public async Task<List<Category>> GetCategories()
    {
        return await repository.ListAllCategories();
    }

    public async Task<Category?> GetCategory(string categoryName)
    {
        return await repository.FindCategoryByName(categoryName);
    }

    public async Task<Category?> AddCategory(string categoryName, string categoryDescription)
    {
        if (string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(categoryDescription))
            return null;
        var c = new Category(categoryName, categoryDescription);
        await repository.SaveCategory(c);
        return c;
    }

    public async Task<bool> DeleteCategory(string categoryName)
    {
        var c = await repository.FindCategoryByName(categoryName);
        if (c == null)
            return false;
        if (c.Articles.Count < 0)
            return false;
        await repository.DeleteCategory(c);
        return true;
    }
}