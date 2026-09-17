using System.ComponentModel.DataAnnotations;

namespace NewsWebApi.DTOs
{
    public class ArticleDTO
    {
        [Required]
        public string HeadLine { get; set; }
        
        [Required]
        public string Body { get; set; }

        public Guid Id { get; set; }

        [Required]
        public AuthorSummeryDTO Author { get; set; }

        [Required]
        public String CategoryName{ get; set; }

        [Required]
        public string? SourceUrl { get; set; }
      
        public DateTime CreatedAt { get; set; }


    }
}
