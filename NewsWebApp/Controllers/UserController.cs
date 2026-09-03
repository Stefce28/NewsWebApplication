using Microsoft.AspNetCore.Mvc;
using NewsWebApp.Services.IServices;

namespace NewsWebApp.Controllers;

[Route("users")]
public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // GET /users
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var users = await _userService.ListAllUsers();

        return View(users);
    }

    // GET /users/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _userService.GetUserById(id);

        if (user == null)
            return NotFound();

        return View(user);
    }

    // GET /users/{id}/edit
    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userService.GetUserById(id);

        if (user == null)
            return NotFound();

        return View(user);
    }

    // POST /users/{id}/edit
    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        string name,
        string surname,
        string email)
    {
        var updatedUser = await _userService.UpdateUser(
            id,
            name,
            surname,
            email
        );

        if (updatedUser == null)
            return NotFound();

        return RedirectToAction(
            nameof(Details),
            new { id = updatedUser.Id }
        );
    }

    // POST /users/{id}/delete
    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool deleted = await _userService.DeleteUser(id);

        if (!deleted)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }
}