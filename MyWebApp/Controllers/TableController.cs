using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.Services;
using QRCoder;

namespace MyWebApp.Controllers;

public sealed class TableController(CafeDataService cafe, IConfiguration configuration) : Controller
{
    [HttpGet("table/{id:int}")]
    public async Task<IActionResult> Index(int id, string session)
    {
        if (string.IsNullOrWhiteSpace(session)) return View(new TablePageViewModel { Token = "", Error = "ลิงก์ QR ไม่มี session กรุณาเรียกพนักงาน" });
        var info = await cafe.GetTableSessionAsync(id, session);
        if (info is null) return View(new TablePageViewModel { Token = session, Error = "QR session หมดอายุ กรุณาเรียกพนักงาน" });
        return View(new TablePageViewModel { SessionInfo = info, Token = session });
    }

    [HttpGet("table/{id:int}/qr")]
    public async Task<IActionResult> Qr(int id, string session)
    {
        if (string.IsNullOrWhiteSpace(session) || await cafe.GetTableSessionAsync(id, session) is null) return NotFound();
        var url = $"{CafeUrlBuilder.GetBaseUrl(Request, configuration)}/table/{id}?session={Uri.EscapeDataString(session)}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return File(new PngByteQRCode(data).GetGraphic(9), "image/png");
    }

    [HttpPost("table/{id:int}/order"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Order(int id, TableOrderForm form)
    {
        var result = form.Items.All(x => x.Quantity <= 0) ? (false, "เลือกอย่างน้อยหนึ่งเมนู") : await cafe.PlaceOrderAsync(id, form);
        TempData[result.Item1 ? "Success" : "Error"] = result.Item1 ? "ส่งรายการไปยังครัวแล้ว" : result.Item2 ?? "สั่งอาหารไม่สำเร็จ";
        return Redirect($"/table/{id}?session={Uri.EscapeDataString(form.Session)}");
    }

    [HttpPost("table/{id:int}/bill"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Bill(int id, string session)
    {
        var result = await cafe.RequestBillAsync(id, session);
        TempData[result.Ok ? "Success" : "Error"] = result.Ok ? "แจ้งแคชเชียร์แล้ว กรุณารอสักครู่" : result.Error ?? "เรียกเก็บเงินไม่สำเร็จ";
        return Redirect($"/table/{id}?session={Uri.EscapeDataString(session)}");
    }
}
