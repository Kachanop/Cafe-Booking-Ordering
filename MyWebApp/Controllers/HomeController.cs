using Microsoft.AspNetCore.Mvc;
using MyWebApp.Services;

namespace MyWebApp.Controllers;

public sealed class HomeController(CafeDataService cafe) : Controller
{
    public async Task<IActionResult> Index() => View(await cafe.GetHomeAsync());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Models.ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
