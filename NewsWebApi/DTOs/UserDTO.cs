namespace NewsWebApi.DTOs
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<ArticleDTO> Posts { get; set; } = new List<ArticleDTO>();

    }
}
