using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.Services;
using QRCoder;

namespace MyWebApp.Controllers;

public sealed class TableController(CafeBackendClient api, IConfiguration configuration) : Controller
{
    [HttpGet("table/{id:int}")]
    public async Task<IActionResult> Index(int id, string session)
    {
        if (string.IsNullOrWhiteSpace(session)) return View(new TablePageViewModel { Token = "", Error = "ลิงก์ QR ไม่มี session กรุณาเรียกพนักงาน" });
        var result = await api.Get<TableSessionDto>($"api/tables/{id}/session?session={Uri.EscapeDataString(session)}");
        if (result.Value is null) return View(new TablePageViewModel { Token = session, Error = result.Error ?? "QR session หมดอายุ" });
        return View(new TablePageViewModel { SessionInfo = result.Value, Token = session });
    }

    [HttpGet("table/{id:int}/qr")]
    public async Task<IActionResult> Qr(int id, string session)
    {
        if (string.IsNullOrWhiteSpace(session)) return BadRequest();
        var valid = await api.Get<TableSessionDto>($"api/tables/{id}/session?session={Uri.EscapeDataString(session)}");
        if (valid.Value is null) return NotFound();
        var baseUrl = (configuration["PublicBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        var url = $"{baseUrl}/table/{id}?session={Uri.EscapeDataString(session)}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return File(new PngByteQRCode(data).GetGraphic(9), "image/png");
    }

    [HttpPost("table/{id:int}/order"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Order(int id, TableOrderForm form)
    {
        var items = form.Items.Where(x => x.Quantity > 0).ToList();
        if (items.Count == 0) TempData["Error"] = "เลือกอย่างน้อยหนึ่งเมนู";
        else
        {
            var response = await api.Send("POST", $"api/tables/{id}/orders", new { session = form.Session, items });
            TempData[response.Ok ? "Success" : "Error"] = response.Ok ? "ส่งรายการไปยังครัวแล้ว" : response.Error ?? "สั่งอาหารไม่สำเร็จ";
        }
        return Redirect($"/table/{id}?session={Uri.EscapeDataString(form.Session)}");
    }

    [HttpPost("table/{id:int}/bill"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Bill(int id, string session)
    {
        var response = await api.Send("POST", $"api/tables/{id}/bill", new { session });
        TempData[response.Ok ? "Success" : "Error"] = response.Ok ? "แจ้งแคชเชียร์แล้ว กรุณารอสักครู่" : response.Error ?? "เรียกเก็บเงินไม่สำเร็จ";
        return Redirect($"/table/{id}?session={Uri.EscapeDataString(session)}");
    }
}
