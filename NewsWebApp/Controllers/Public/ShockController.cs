using Microsoft.AspNetCore.Mvc;
using NewsWebApp.Services.IServices;

namespace NewsWebApp.Controllers;

[Route("shock")]
public class ShockController : Controller
{
    private readonly IShockService _shockService;

    public ShockController(IShockService shockService)
    {
        _shockService = shockService;
    }

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        _shockService.ResetShock();

        return NoContent();
    }
}
