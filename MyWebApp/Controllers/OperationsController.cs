using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers;

public sealed class OperationsController(CafeDataService cafe) : Controller
{
    [HttpGet("admin")]
    public async Task<IActionResult> Admin()
    {
        if (!Allowed("Manager", "Owner")) return Denied();
        return View(await cafe.GetAdminAsync(Role));
    }

    [HttpPost("admin/menu/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> MenuCreate(MenuForm form) => await SaveMenu(form, "เพิ่มเมนูแล้ว");

    [HttpPost("admin/menu/update"), ValidateAntiForgeryToken]
    public async Task<IActionResult> MenuUpdate(MenuForm form) => await SaveMenu(form, "บันทึกเมนูแล้ว");

    [HttpPost("admin/services/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceCreate(ServiceForm form) => await SaveService(form, "เพิ่มบริการแล้ว");

    [HttpPost("admin/services/update"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceUpdate(ServiceForm form) => await SaveService(form, "บันทึกบริการแล้ว");

    [HttpPost("admin/users/role"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(RoleForm form)
    {
        if (!Allowed("Owner")) return Denied();
        var result = await cafe.UpdateRoleAsync(HttpContext.Session.GetInt32("Cafe.UserId")!.Value, form);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "บันทึกสิทธิ์แล้ว" : result.Error ?? "เปลี่ยนสิทธิ์ไม่สำเร็จ";
        return RedirectToAction(nameof(Admin));
    }

    [HttpGet("tables")]
    public async Task<IActionResult> Tables()
    {
        if (!Allowed("Waiter", "Cashier", "Manager", "Owner")) return Denied();
        return View(await cafe.GetTablesAsync());
    }

    [HttpPost("tables/{id:int}/open"), ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenTable(int id)
    {
        if (!Allowed("Waiter", "Cashier", "Manager", "Owner")) return Denied();
        var result = await cafe.OpenTableAsync(id);
        if (result.Table is not null)
        {
            var url = new Uri(new Uri(Request.Scheme + "://" + Request.Host), result.Table.Url.TrimStart('/')).ToString();
            TempData["OpenedTableUrl"] = url;
            TempData["OpenedTableNumber"] = result.Table.TableNumber;
            TempData["OpenedTableId"] = result.Table.Id;
            TempData["OpenedTableSession"] = result.Table.Session;
        }
        else TempData["Error"] = result.Error ?? "เปิดโต๊ะไม่สำเร็จ";
        return RedirectToAction(nameof(Tables));
    }

    [HttpGet("kds")]
    public async Task<IActionResult> Kds(bool summary = false)
    {
        if (!Allowed("Kitchen", "Manager", "Owner")) return Denied();
        return View(await cafe.GetKdsAsync(summary));
    }

    [HttpPost("kds/{id:int}/status"), ValidateAntiForgeryToken]
    public async Task<IActionResult> KitchenStatus(int id, string status, bool summary = false)
    {
        if (!Allowed("Kitchen", "Manager", "Owner")) return Denied();
        var result = await cafe.SetKitchenStatusAsync(id, status);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "อัปเดตสถานะแล้ว" : result.Error ?? "อัปเดตสถานะไม่สำเร็จ";
        return RedirectToAction(nameof(Kds), new { summary });
    }

    [HttpGet("cashier")]
    public async Task<IActionResult> Cashier()
    {
        if (!Allowed("Cashier", "Manager", "Owner")) return Denied();
        return View(await cafe.GetCashierAsync());
    }

    [HttpPost("cashier/close"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseTable(int tableId, string method)
    {
        if (!Allowed("Cashier", "Manager", "Owner")) return Denied();
        var result = await cafe.CloseTableAsync(tableId, method);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "รับชำระและปิดโต๊ะแล้ว" : result.Error ?? "ปิดบิลไม่สำเร็จ";
        return RedirectToAction(nameof(Cashier));
    }

    [HttpPost("cashier/bookings/{id:int}/paid"), ValidateAntiForgeryToken]
    public async Task<IActionResult> BookingPaid(int id, string method)
    {
        if (!Allowed("Cashier", "Manager", "Owner")) return Denied();
        var result = await cafe.MarkBookingPaidAsync(id, method);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "บันทึกการชำระเงินแล้ว" : result.Error ?? "ยืนยันไม่สำเร็จ";
        return Redirect(Role == "Cashier" ? "/cashier" : "/admin");
    }

    [HttpGet("artist-dashboard")]
    public async Task<IActionResult> ArtistDashboard()
    {
        if (!Allowed("Artist", "Manager", "Owner")) return Denied();
        var userId = HttpContext.Session.GetInt32("Cafe.UserId")!.Value;
        return View(await cafe.GetBookingsAsync(userId, Role));
    }

    [HttpPost("artist/bookings/{id:int}/complete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteBooking(int id)
    {
        if (!Allowed("Artist", "Manager", "Owner")) return Denied();
        var result = await cafe.CompleteBookingAsync(id, HttpContext.Session.GetInt32("Cafe.UserId")!.Value, Role);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "ปิดงานเรียบร้อย" : result.Error ?? "เปลี่ยนสถานะไม่สำเร็จ";
        return RedirectToAction(nameof(ArtistDashboard));
    }

    [HttpGet("it")]
    public async Task<IActionResult> It()
    {
        if (!Allowed("IT Staff", "Manager", "Owner")) return Denied();
        return View(await cafe.GetItOverviewAsync());
    }

    private async Task<IActionResult> SaveMenu(MenuForm form, string success)
    {
        if (!Allowed("Manager", "Owner")) return Denied();
        var result = await cafe.SaveMenuAsync(form);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? success : result.Error ?? "บันทึกไม่สำเร็จ";
        return RedirectToAction(nameof(Admin));
    }

    private async Task<IActionResult> SaveService(ServiceForm form, string success)
    {
        if (!Allowed("Manager", "Owner")) return Denied();
        var result = await cafe.SaveServiceAsync(form);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? success : result.Error ?? "บันทึกไม่สำเร็จ";
        return RedirectToAction(nameof(Admin));
    }

    private string Role => HttpContext.Session.GetString("Cafe.Role") ?? "";
    private bool Allowed(params string[] roles) => HttpContext.Session.GetInt32("Cafe.UserId") is not null && roles.Contains(Role, StringComparer.OrdinalIgnoreCase);
    private IActionResult Denied() => HttpContext.Session.GetInt32("Cafe.UserId") is null
        ? RedirectToAction("Login", "Account", new { returnUrl = Request.Path + Request.QueryString })
        : Forbid();
}
