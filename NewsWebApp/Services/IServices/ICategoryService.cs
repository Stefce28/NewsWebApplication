using NewsWebApp.Models;

namespace NewsWebApp.Services.IServices;

public interface ICategoryService
{
    Task<List<Category>> GetCategories();
    Task<Category?> GetCategory(string categoryName);
    Task<Category?> AddCategory(string categoryName,  string categoryDescription);
    Task<bool> DeleteCategory(string categoryName);
}