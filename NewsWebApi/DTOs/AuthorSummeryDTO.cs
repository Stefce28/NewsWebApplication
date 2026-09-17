using System.ComponentModel.DataAnnotations;

namespace NewsWebApi.DTOs
{
    public class AuthorSummeryDTO
    {
        
        public Guid Id{ get; set; }
        [Required]
        public String Name { get; set; }
    }
}
