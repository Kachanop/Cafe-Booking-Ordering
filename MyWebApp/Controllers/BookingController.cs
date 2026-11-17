using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers;

public sealed class BookingController(CafeDataService cafe) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date = null, int staffId = 0)
    {
        var selected = date?.Date ?? DateTime.Today;
        return View(await cafe.GetBookingPageAsync(selected, staffId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingForm form)
    {
        var userId = HttpContext.Session.GetInt32("Cafe.UserId");
        if (userId is null) return RedirectToAction("Login", "Account", new { returnUrl = "/booking" });
        if (form.BookingDate.Date < DateTime.Today) ModelState.AddModelError(nameof(form.BookingDate), "เลือกวันที่ตั้งแต่วันนี้เป็นต้นไป");
        if (!ModelState.IsValid) return View("Index", await cafe.GetBookingPageAsync(form.BookingDate, form.StaffId, form));
        var result = await cafe.CreateBookingAsync(userId.Value, HttpContext.Session.GetString("Cafe.Role") ?? "", form);
        if (!result.Ok) { ModelState.AddModelError(string.Empty, result.Error ?? "จองคิวไม่สำเร็จ"); return View("Index", await cafe.GetBookingPageAsync(form.BookingDate, form.StaffId, form)); }
        TempData["Success"] = "ส่งคำขอจองแล้ว กรุณาชำระเต็มจำนวนที่แคชเชียร์เพื่อยืนยันคิว";
        return RedirectToAction(nameof(History));
    }

    [HttpGet("my-bookings")]
    public async Task<IActionResult> History()
    {
        var userId = HttpContext.Session.GetInt32("Cafe.UserId");
        if (userId is null) return RedirectToAction("Login", "Account", new { returnUrl = "/my-bookings" });
        var role = HttpContext.Session.GetString("Cafe.Role") ?? "";
        if (role is not ("Member" or "Artist" or "Waiter" or "Cashier" or "Manager" or "Owner")) return Forbid();
        return View(await cafe.GetBookingsAsync(userId.Value, role));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = HttpContext.Session.GetInt32("Cafe.UserId");
        var role = HttpContext.Session.GetString("Cafe.Role") ?? "";
        if (userId is null) return RedirectToAction("Login", "Account");
        var result = await cafe.CancelBookingAsync(id, userId.Value, role);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "ยกเลิกรายการจองแล้ว" : result.Error ?? "ยกเลิกไม่สำเร็จ";
        return RedirectToAction(nameof(History));
    }
}
