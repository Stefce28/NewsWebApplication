using System.ComponentModel.DataAnnotations;

namespace NewsWebApp.Models;

public class Article
{
    [Required]
    public string HeadLine { get; set; } = string.Empty;
    [Required]
    public string Body  { get; set; } = string.Empty;
    [Key]
    public Guid Pid  { get; set; }
    public User Author { get; set; } = null!;
    public Category Category { get; set; } = null!;
    
    public Article()
    {
    }
    
    public Article(string headLine, string body, Category category, User author)
    {
        this.HeadLine = headLine;
        this.Body = body;
        this.Pid = Guid.NewGuid();
        this.Author = author;
        this.Category = category;
    }
}
