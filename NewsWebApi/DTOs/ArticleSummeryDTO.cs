namespace NewsWebApi.DTOs
{
    public class ArticleSummeryDTO
    {
        public Guid Id { get; set; }
        public string HeadLine { get; set; }
        public string AuthorName { get; set; }
        public decimal ShockValue { get; set; }
        public string CategoryName { get; set; }
    }
}
