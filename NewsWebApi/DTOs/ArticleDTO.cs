using System.ComponentModel.DataAnnotations;

namespace NewsWebApi.DTOs
{
    public class ArticleDTO
    {
        public string HeadLine { get; set; }

        public string Body { get; set; }

        public Guid Id { get; set; }
        public AuthorSummeryDTO Author { get; set; }
        public String CategoryName{ get; set; }
      
        public DateTime CreatedAt { get; set; }


    }
}