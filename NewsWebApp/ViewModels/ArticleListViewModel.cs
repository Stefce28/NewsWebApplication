using NewsWebApp.Models;

namespace NewsWebApp.ViewModels;

public class ArticleListViewModel
{
    public List<Article> Articles { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? CategoryName { get; set; }
    public Guid? AuthorId { get; set; }
    public string? Title { get; set; }
}
