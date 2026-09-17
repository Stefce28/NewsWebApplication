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

    [HttpGet("status")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status()
    {
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        var status = _shockService.GetShockStatus();

        return Json(new
        {
            status.CurrentPercentage,
            status.LevelClass,
            status.IsInCooldown,
            status.CooldownEndsAtUnixMilliseconds,
            status.OverloadMessage
        });
    }

    [HttpPost("reset")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Reset()
    {
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        _shockService.ResetShock();

        return NoContent();
    }

}
