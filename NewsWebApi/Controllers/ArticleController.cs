using Microsoft.AspNetCore.Mvc;
using NewsWebApi.DTOs;
using NewsWebApi.Service;
using NewsWebApp.Services.IServices;

namespace NewsWebApi.Controllers
{
    [ApiController]
    [Route("api/articles")]
    public class ArticleController : ControllerBase
    {
        private readonly ArticleApiService _articleApiService;
        private readonly IArticleService _articleService;

        public ArticleController(IArticleService articleService, ArticleApiService articleApiService)
        {
            _articleService = articleService;
            _articleApiService = articleApiService;

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleDTO>> GetById(Guid id)
        {
            var article = await _articleService.GetArticleById(id);
            if (article == null)
            {
                return NotFound();
            }
            var dto = new ArticleDTO
            {
                HeadLine = article.HeadLine,
                Body = article.Body,
                Id = article.Pid,
                Author = new AuthorSummeryDTO
                {
                    Id = article.Author.Id,
                    Name = article.Author.Name
                },
                CategoryName = article.Category.Name,
                CreatedAt = article.CreatedAt
            };
            return Ok(dto);
        }

        [HttpPost]
        [Route("/add")]
        public async Task<ActionResult<ArticleDTO>> CreateArticleDto(CreateArticleDTO dto)
        {
            var article =  await _articleApiService.CretaeArticleDTO(dto);
            return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);

        }
    }
}
