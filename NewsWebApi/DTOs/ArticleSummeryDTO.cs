using System.ComponentModel.DataAnnotations;

namespace NewsWebApi.DTOs
{
    public class ArticleSummeryDTO
    {
        public Guid Id { get; set; }
        [Required]
        public string HeadLine { get; set; }
        [Required]
        public string AuthorName { get; set; }
        [Required]
        public decimal ShockValue { get; set; }
        [Required]
        public string CategoryName { get; set; }
    }
}
