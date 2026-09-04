using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsWebApp.Services.IServices;

namespace NewsWebApp.Controllers;

[Route("articles")]
public class ArticlesController : Controller
{
    private const int PageSize = 9;

    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly IUserService _userService;

    public ArticlesController(
        IArticleService articleService,
        ICategoryService categoryService,
        IUserService userService)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _userService = userService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var model = await _articleService.GetArticleList(
            page,
            PageSize,
            title: "Најнови вести"
        );

        return View(model);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var article = await _articleService.GetArticleById(id);

        if (article == null)
            return NotFound();

        return View(article);
    }
    
    [HttpGet("category/{categoryName}")]
    public async Task<IActionResult> Category(string categoryName, int page = 1)
    {
        var category = await _categoryService.GetCategory(categoryName);

        if (category == null)
            return NotFound();

        var model = await _articleService.GetArticleList(
            page,
            PageSize,
            categoryName: categoryName,
            title: category.Name
        );

        return View("Index", model);
    }

    [HttpGet("author/{authorId:guid}")]
    public async Task<IActionResult> ByAuthor(Guid authorId, int page = 1)
    {
        var author = await _userService.GetUserById(authorId);

        if (author == null)
            return NotFound();

        var model = await _articleService.GetArticleList(
            page,
            PageSize,
            authorId: authorId,
            title: $"Вести од {author.Name} {author.Surname}"
        );

        return View("Index", model);
    }

    private async Task LoadArticleFormOptions()
    {
        ViewBag.Categories =
            await _categoryService.GetCategories();

        ViewBag.Users =
            await _userService.ListAllUsers();
    }

    
    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        await LoadArticleFormOptions();

        return View();
    }

    [Authorize]
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string headLine,
        string body,
        string categoryName,
        Guid authorId)
    {
        await LoadArticleFormOptions();

        var category =
            await _categoryService.GetCategory(categoryName);

        if (category == null)
        {
            ModelState.AddModelError(
                "",
                "Category does not exist."
            );

            return View();
        }

        var author =
            await _userService.GetUserById(authorId);

        if (author == null)
        {
            ModelState.AddModelError(
                "",
                "Author does not exist."
            );

            return View();
        }

        var article =
            await _articleService.AddArticleAsync(
                headLine,
                body,
                category,
                author
            );

        if (article == null)
        {
            ModelState.AddModelError(
                "",
                "Invalid article data."
            );

            return View();
        }

        return RedirectToAction(
            nameof(Details),
            new { id = article.Pid }
        );
    }

    [Authorize]
    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var article =
            await _articleService.GetArticleById(id);

        if (article == null)
            return NotFound();

        await LoadArticleFormOptions();

        return View(article);
    }

    [Authorize]
    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        string headLine,
        string body,
        string categoryName,
        Guid authorId)
    {
        var article = await _articleService.GetArticleById(id);

        if (article == null)
            return NotFound();

        await LoadArticleFormOptions();

        var category =
            await _categoryService.GetCategory(categoryName);

        if (category == null)
        {
            ModelState.AddModelError(
                "",
                "Category does not exist."
            );

            return View(article);
        }

        var author =
            await _userService.GetUserById(authorId);

        if (author == null)
        {
            ModelState.AddModelError(
                "",
                "Author does not exist."
            );

            return View(article);
        }

        var updatedArticle =
            await _articleService.UpdateArticle(
                id,
                headLine,
                body,
                category,
                author
            );

        if (updatedArticle == null)
        {
            ModelState.AddModelError(
                "",
                "Invalid article data."
            );

            return View(article);
        }

        return RedirectToAction(
            nameof(Details),
            new { id = updatedArticle.Pid }
        );
    }

    [Authorize]
    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool deleted =
            await _articleService.DeleteArticleAsync(id);

        if (!deleted)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }
}
