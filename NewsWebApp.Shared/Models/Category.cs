using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NewsWebApp.Shared.Models;
public class Category
{
    [Key]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;

    public List<Article> Articles { get; set; } = new();

    public Category() { }

    public Category(string categoryName, string categoryDescription)
    {
        this.Name = categoryName;
        this.Description = categoryDescription;
    }
}

