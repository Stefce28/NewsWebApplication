using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NewsWebApp.Shared.Models;

namespace NewsWebApp.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    
    [HttpGet("register")]
    public IActionResult Register()
    {
        return View();
    }
    
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string name,
        string surname,
        string email,
        string password)
    {
        var user = new User(
            name,
            surname,
            email
        );

        var result =
            await _userManager.CreateAsync(
                user,
                password
            );

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description
                );
            }

            return View();
        }

        await _signInManager.SignInAsync(
            user,
            isPersistent: false
        );

        return RedirectToAction(
            "Index",
            "Articles"
        );
    }
    
    [HttpGet("login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string email,
        string password)
    {
        var result =
            await _signInManager.PasswordSignInAsync(
                email,
                password,
                isPersistent: false,
                lockoutOnFailure: false
            );

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                "",
                "Invalid email or password."
            );

            return View();
        }

        return RedirectToAction(
            "Index",
            "Articles"
        );
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(
            "Index",
            "Articles"
        );
    }
}