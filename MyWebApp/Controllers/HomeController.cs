using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers;

public sealed class HomeController(CafeBackendClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var menuTask = api.Get<List<MenuItemDto>>("api/menu"); var artistTask = api.Get<List<ArtistDto>>("api/artists"); var serviceTask = api.Get<List<ServiceDto>>("api/services");
        await Task.WhenAll(menuTask, artistTask, serviceTask);
        var menu = await menuTask; var artists = await artistTask; var services = await serviceTask;
        return View(new HomeViewModel { Menu = menu.Value ?? [], Artists = artists.Value ?? [], Services = services.Value ?? [], Error = menu.Error ?? artists.Error ?? services.Error });
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
