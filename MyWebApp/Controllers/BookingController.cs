using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers;

public sealed class BookingController(CafeBackendClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date = null, int staffId = 0)
    {
        var selected = date?.Date ?? DateTime.Today;
        var page = await BookingPage(selected, staffId);
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingForm form)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Cafe.Token"))) return RedirectToAction("Login", "Account", new { returnUrl = "/booking" });
        if (form.BookingDate.Date < DateTime.Today) ModelState.AddModelError(nameof(form.BookingDate), "เลือกวันที่ตั้งแต่วันนี้เป็นต้นไป");
        if (!ModelState.IsValid) return View("Index", await BookingPage(form.BookingDate, form.StaffId, form));
        var result = await api.Send("POST", "api/bookings", form);
        if (!result.Ok) { ModelState.AddModelError(string.Empty, result.Error ?? "จองคิวไม่สำเร็จ"); return View("Index", await BookingPage(form.BookingDate, form.StaffId, form)); }
        TempData["Success"] = "ส่งคำขอจองแล้ว กรุณาชำระเต็มจำนวนที่แคชเชียร์เพื่อยืนยันคิว"; return RedirectToAction("History");
    }

    [HttpGet("my-bookings")]
    public async Task<IActionResult> History()
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("Cafe.Token"))) return RedirectToAction("Login", "Account", new { returnUrl = "/my-bookings" });
        var result = await api.Get<List<BookingDto>>("api/bookings"); if (result.Status == System.Net.HttpStatusCode.Unauthorized) return RedirectToAction("Login", "Account");
        ViewBag.Error = result.Error; return View(result.Value ?? []);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await api.Send("POST", $"api/bookings/{id}/cancel"); TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "ยกเลิกรายการจองแล้ว" : result.Error ?? "ยกเลิกไม่สำเร็จ"; return RedirectToAction("History");
    }

    private async Task<BookingPageViewModel> BookingPage(DateTime date, int staffId, BookingForm? form = null)
    {
        var artistsTask = api.Get<List<ArtistDto>>("api/artists"); var servicesTask = api.Get<List<ServiceDto>>("api/services"); Task<(List<SlotDto>? Value, string? Error, System.Net.HttpStatusCode Status)> slotsTask = Task.FromResult<(List<SlotDto>?, string?, System.Net.HttpStatusCode)>(([], null, System.Net.HttpStatusCode.OK));
        if (staffId > 0) slotsTask = api.Get<List<SlotDto>>($"api/availability?staffId={staffId}&date={date:yyyy-MM-dd}");
        await Task.WhenAll(artistsTask, servicesTask, slotsTask);
        return new BookingPageViewModel { Artists = (await artistsTask).Value ?? [], Services = (await servicesTask).Value ?? [], Slots = (await slotsTask).Value ?? [], Form = form ?? new BookingForm { BookingDate = date, StaffId = staffId }, Error = (await artistsTask).Error ?? (await servicesTask).Error ?? (await slotsTask).Error };
    }
}
