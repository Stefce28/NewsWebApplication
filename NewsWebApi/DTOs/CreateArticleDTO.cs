using System.ComponentModel.DataAnnotations;

namespace NewsWebApi.DTOs
{
    public class CreateArticleDTO
    {
        [Required]
        public string HeadLine { get; set; } = string.Empty;
        
        [Required]
        public string Body { get; set; } = string.Empty;
        
        [Required]
        public string CategoryId { get; set; } = string.Empty;
        public Guid? AuthorId { get; set; }
        [Required]
        public string? AuthorEmail { get; set; }
        [Required]
        public string? SourceUrl { get; set; }
        [Required]
        public decimal ShockValue { get; set; }

    }
}
