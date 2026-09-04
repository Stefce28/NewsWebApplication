using Microsoft.AspNetCore.Mvc;
using NewsWebApp.Services.IServices;

namespace NewsWebApp.Controllers;

[Route("categories")]
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoryController(
        ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var categories =
            await _categoryService.GetCategories();

        return View(categories);
    }

    
    [HttpGet("{name}")]
    public async Task<IActionResult> Details(string name)
    {
        var category =
            await _categoryService.GetCategory(name);

        if (category == null)
            return NotFound();

        return View(category);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string categoryName,
        string categoryDescription)
    {
        var category =
            await _categoryService.AddCategory(
                categoryName,
                categoryDescription
            );

        if (category == null)
        {
            ModelState.AddModelError(
                "",
                "Invalid category data."
            );

            return View();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{name}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string name)
    {
        bool deleted =
            await _categoryService.DeleteCategory(name);

        if (!deleted)
        {
            ModelState.AddModelError(
                "",
                "Category could not be deleted."
            );

            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }
}