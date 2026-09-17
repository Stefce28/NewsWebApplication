using NewsWebApi.DTOs;
using NewsWebApp.Services.IServices;
using NewsWebApp.Shared.Models;

namespace NewsWebApi.Service
{
    public class ArticleApiService
    {
        private readonly IArticleService _articleService;
        private readonly IUserService _userService;
        private readonly ICategoryService _categoryService;

        public ArticleApiService(
            IArticleService articleService,
            IUserService userService,
            ICategoryService categoryService)
        {
            _articleService = articleService;
            _userService = userService;
            _categoryService = categoryService;
        }

        public async Task<(ArticleDTO Article, bool Created)> CreateArticleDTO(CreateArticleDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.HeadLine)) throw new ArgumentException("Headline is required");
            if (string.IsNullOrWhiteSpace(dto.Body)) throw new ArgumentException("Body is required");
            if (string.IsNullOrWhiteSpace(dto.CategoryId)) throw new ArgumentException("CategoryId is required");

            var headline = dto.HeadLine.Trim();
            var body = dto.Body.Trim();
            var categoryId = dto.CategoryId.Trim();
            var sourceUrl = NormalizeSourceUrl(dto.SourceUrl);

            var existingArticle = await FindExistingArticle(headline, sourceUrl);
            if (existingArticle != null)
            {
                return (MapArticle(existingArticle), false);
            }

            var author = await ResolveAuthor(dto);
            var category = await _categoryService.GetCategory(categoryId);

            if (category == null) throw new ArgumentException("Category not found");
            if (author == null) throw new ArgumentException("Author not found");

            var article = await _articleService.AddArticleAsync(
                headline,
                body,
                category,
                author,
                dto.ShockValue,
                sourceUrl);

            if (article == null) throw new InvalidOperationException("Article could not be created");

            return (MapArticle(article), true);
        }

        private async Task<Article?> FindExistingArticle(string headline, string? sourceUrl)
        {
            if (!string.IsNullOrWhiteSpace(sourceUrl))
            {
                var existingBySource = await _articleService.GetArticleBySourceUrl(sourceUrl);
                if (existingBySource != null)
                {
                    return existingBySource;
                }
            }

            return await _articleService.GetArticleByHeadline(headline);
        }

        private async Task<User?> ResolveAuthor(CreateArticleDTO dto)
        {
            if (dto.AuthorId is { } authorId && authorId != Guid.Empty)
            {
                return await _userService.GetUserById(authorId);
            }

            if (!string.IsNullOrWhiteSpace(dto.AuthorEmail))
            {
                return await _userService.GetUserByEmail(dto.AuthorEmail.Trim());
            }

            return null;
        }

        private static string? NormalizeSourceUrl(string? sourceUrl)
        {
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            var builder = new UriBuilder(uri)
            {
                Fragment = string.Empty
            };

            if ((builder.Scheme == Uri.UriSchemeHttp && builder.Port == 80) ||
                (builder.Scheme == Uri.UriSchemeHttps && builder.Port == 443))
            {
                builder.Port = -1;
            }

            return builder.Uri.AbsoluteUri.TrimEnd('/');
        }

        private static ArticleDTO MapArticle(Article article)
        {
            return new ArticleDTO
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
        }
    }
}
