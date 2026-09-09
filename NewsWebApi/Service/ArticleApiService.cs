using NewsWebApi.DTOs;
using NewsWebApp.Services.IServices;

namespace NewsWebApi.Service
{
    public class ArticleApiService
    {
        private readonly IArticleService _articleService;
        private readonly IUserService _userService;

        private readonly ICategoryService _categoryService;

        public ArticleApiService(IArticleService articleService, IUserService userService, ICategoryService categoryService)
        {
            _articleService = articleService;
            _userService = userService;
            _categoryService = categoryService;
        }

        public async Task<ArticleDTO> CretaeArticleDTO(CreateArticleDTO dot)
        {
            var author =  _userService.GetUserById(dot.AuthorId).Result;
            var category =  _categoryService.GetCategory(dot.CategoryId).Result;

            if (category == null) throw new ArgumentException("Category not found");
            if (author == null) throw new ArgumentException("Author not found");

            var article = _articleService.AddArticleAsync(dot.HeadLine, dot.Body, category, author, dot.ShockValue).Result;

            return new ArticleDTO
            {
                HeadLine = dot.HeadLine,
                Body = dot.Body,
                Id = article.Pid,
                Author = new AuthorSummeryDTO
                {
                    Id = author.Id,
                    Name = author?.Name ?? "Unknown",
                },
                CategoryName = category?.Name ?? "Unknown",
                CreatedAt = DateTime.UtcNow
            };
        } 

    }
}
