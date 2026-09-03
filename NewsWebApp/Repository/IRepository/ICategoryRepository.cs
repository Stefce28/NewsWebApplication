using NewsWebApp.Models;

namespace NewsWebApp.Repository;

public interface ICategoryRepository 
{
    Task<List<Category>> ListAllCategories();
    Task<Category?> FindCategoryByName(string categoryName);
    Task SaveCategory(Category category);
    Task DeleteCategory(Category category);
}