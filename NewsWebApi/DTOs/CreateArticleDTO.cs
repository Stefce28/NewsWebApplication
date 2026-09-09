namespace NewsWebApi.DTOs
{
    public class CreateArticleDTO
    {
        public string HeadLine { get; set; }

        public string Body { get; set; }

        public String CategoryId { get; set; }
        public Guid AuthorId { get; set; }
        public decimal ShockValue { get; set; }

    }
}
