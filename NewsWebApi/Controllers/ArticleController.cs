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
                SourceUrl = article.SourceUrl,
                CreatedAt = article.CreatedAt
            };
            return Ok(dto);
        }

        [HttpPost("add")]
        public async Task<ActionResult<ArticleDTO>> CreateArticleDto(CreateArticleDTO dto)
        {
            try
            {
                var result = await _articleApiService.CreateArticleDTO(dto);

                if (!result.Created)
                {
                    return Ok(result.Article);
                }

                return CreatedAtAction(nameof(GetById), new { id = result.Article.Id }, result.Article);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }

        }
    }
}
