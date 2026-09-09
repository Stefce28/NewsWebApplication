
namespace NewsWebApi.DTOs
{
    public class CategoryDTO
    {

       public string Name { get; set; }
       public string Description { get; set; }
       public string CategoryId { get; set; }

       public List<ArticleSummeryDTO> Articles { get; set; } = new List<ArticleSummeryDTO>();

    }
}
