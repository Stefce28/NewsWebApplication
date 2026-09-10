using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
namespace NewsWebApp.Shared.Models;

public class Article
{
    [Required]
    public string HeadLine { get; set; } = string.Empty;
    [Required]
    public string Body { get; set; } = string.Empty;
    [Key]
    public Guid Pid { get; set; }
    public User Author { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal ShockValue { get; set; } = 0.0m;

    public Article()
    {

    }

    public Article(string headLine, string body, Category category, User author, decimal shockValue)
    {
        this.HeadLine = headLine;
        this.Body = body;
        this.Pid = Guid.NewGuid();
        this.Author = author;
        this.ShockValue = shockValue;
        this.Category = category;
    }
}

