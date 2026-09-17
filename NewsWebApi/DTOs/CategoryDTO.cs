
using System.ComponentModel.DataAnnotations;

namespace NewsWebApi.DTOs
{
    public class CategoryDTO
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string CategoryId { get; set; }

       public List<ArticleSummeryDTO> Articles { get; set; } = new List<ArticleSummeryDTO>();

    }
}
