using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers;

public sealed class OperationsController(CafeBackendClient api) : Controller
{
    [HttpGet("admin")]
    public async Task<IActionResult> Admin()
    {
        if (!Allowed("Manager", "Owner")) return Denied();
        var overviewTask=api.Get<OverviewDto>("api/admin/overview"); var bookingsTask=api.Get<List<BookingDto>>("api/bookings"); var menuTask=api.Get<List<MenuItemDto>>("api/admin/menu"); var servicesTask=api.Get<List<ServiceDto>>("api/admin/services");
        var tasks=new List<Task>{overviewTask,bookingsTask,menuTask,servicesTask}; Task<(List<StaffDto>? Value,string? Error,System.Net.HttpStatusCode Status)>? staffTask=null;
        if (Role=="Owner") { staffTask=api.Get<List<StaffDto>>("api/admin/users"); tasks.Add(staffTask); }
        await Task.WhenAll(tasks);
        return View(new AdminViewModel { Overview=overviewTask.Result.Value, Bookings=bookingsTask.Result.Value??[], Menu=menuTask.Result.Value??[], Services=servicesTask.Result.Value??[], Staff=staffTask?.Result.Value??[] });
    }

    [HttpPost("admin/menu/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> MenuCreate(MenuForm form) { if(!Allowed("Manager","Owner"))return Denied(); var result=await api.Send("POST","api/admin/menu",form);TempData[result.Ok?"Success":"Error"]=result.Ok?"เพิ่มเมนูแล้ว":result.Error??"เพิ่มเมนูไม่สำเร็จ";return RedirectToAction(nameof(Admin)); }
    [HttpPost("admin/menu/update"), ValidateAntiForgeryToken]
    public async Task<IActionResult> MenuUpdate(MenuForm form) { if(!Allowed("Manager","Owner"))return Denied();var result=await api.Send("PUT",$"api/admin/menu/{form.Id}",form);TempData[result.Ok?"Success":"Error"]=result.Ok?"บันทึกเมนูแล้ว":result.Error??"บันทึกไม่สำเร็จ";return RedirectToAction(nameof(Admin)); }
    [HttpPost("admin/services/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceCreate(ServiceForm form) { if(!Allowed("Manager","Owner"))return Denied();var result=await api.Send("POST","api/admin/services",form);TempData[result.Ok?"Success":"Error"]=result.Ok?"เพิ่มบริการแล้ว":result.Error??"เพิ่มบริการไม่สำเร็จ";return RedirectToAction(nameof(Admin)); }
    [HttpPost("admin/services/update"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceUpdate(ServiceForm form) { if(!Allowed("Manager","Owner"))return Denied();var result=await api.Send("PUT",$"api/admin/services/{form.Id}",form);TempData[result.Ok?"Success":"Error"]=result.Ok?"บันทึกบริการแล้ว":result.Error??"บันทึกไม่สำเร็จ";return RedirectToAction(nameof(Admin)); }
    [HttpPost("admin/users/role"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(RoleForm form) { if(!Allowed("Owner"))return Denied();var result=await api.Send("PUT",$"api/admin/users/{form.Id}/role",new{form.Role,form.Bio,form.PortfolioUrl,Specialties=Array.Empty<string>()});TempData[result.Ok?"Success":"Error"]=result.Ok?"บันทึกสิทธิ์แล้ว":result.Error??"เปลี่ยนสิทธิ์ไม่สำเร็จ";return RedirectToAction(nameof(Admin)); }

    [HttpGet("tables")]
    public async Task<IActionResult> Tables() { if(!Allowed("Waiter","Cashier","Manager","Owner"))return Denied();var result=await api.Get<List<TableDto>>("api/tables");ViewBag.Error=result.Error;return View(result.Value??[]); }
    [HttpPost("tables/{id:int}/open"), ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenTable(int id) { if(!Allowed("Waiter","Cashier","Manager","Owner"))return Denied();var result=await api.Send<OpenedTableDto>(HttpMethod.Post,$"api/tables/{id}/open",new{});if(result.Value is not null){var url=new Uri(new Uri(Request.Scheme+"://"+Request.Host),result.Value.Url.TrimStart('/')).ToString();TempData["OpenedTableUrl"]=url;TempData["OpenedTableNumber"]=result.Value.TableNumber;TempData["OpenedTableId"]=result.Value.Id;TempData["OpenedTableSession"]=result.Value.Session;}else TempData["Error"]=result.Error??"เปิดโต๊ะไม่สำเร็จ";return RedirectToAction(nameof(Tables)); }

    [HttpGet("kds")]
    public async Task<IActionResult> Kds(bool summary=false) { if(!Allowed("Kitchen","Manager","Owner"))return Denied();if(summary){var result=await api.Get<List<KitchenSummaryDto>>("api/kds/summary");return View(new KdsViewModel{SummaryItems=result.Value??[],Summary=true});}var items=await api.Get<List<KitchenItemDto>>("api/kds");return View(new KdsViewModel{Items=items.Value??[]}); }
    [HttpPost("kds/{id:int}/status"), ValidateAntiForgeryToken]
    public async Task<IActionResult> KitchenStatus(int id,string status,bool summary=false) { if(!Allowed("Kitchen","Manager","Owner"))return Denied();var result=await api.Send("POST",$"api/kds/{id}/status",new{status});TempData[result.Ok?"Success":"Error"]=result.Ok?"อัปเดตสถานะแล้ว":result.Error??"อัปเดตสถานะไม่สำเร็จ";return RedirectToAction(nameof(Kds),new{summary}); }

    [HttpGet("cashier")]
    public async Task<IActionResult> Cashier() { if(!Allowed("Cashier","Manager","Owner"))return Denied();var billsTask=api.Get<List<CashierRow>>("api/cashier");var bookingsTask=api.Get<List<BookingDto>>("api/bookings");await Task.WhenAll(billsTask,bookingsTask);ViewBag.Error=billsTask.Result.Error;return View(new CashierViewModel{Bills=billsTask.Result.Value??[],PendingBookings=(bookingsTask.Result.Value??[]).Where(x=>x.Status=="Pending").ToList()}); }
    [HttpPost("cashier/close"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseTable(int tableId,string method) { if(!Allowed("Cashier","Manager","Owner"))return Denied();var result=await api.Send("POST",$"api/tables/{tableId}/close",new{method});TempData[result.Ok?"Success":"Error"]=result.Ok?"รับชำระและปิดโต๊ะแล้ว":result.Error??"ปิดบิลไม่สำเร็จ";return RedirectToAction(nameof(Cashier)); }
    [HttpPost("cashier/bookings/{id:int}/paid"), ValidateAntiForgeryToken]
    public async Task<IActionResult> BookingPaid(int id,string method) { if(!Allowed("Cashier","Manager","Owner"))return Denied();var result=await api.Send("POST",$"api/bookings/{id}/paid",new{method});TempData[result.Ok?"Success":"Error"]=result.Ok?"บันทึกการชำระเงินแล้ว":result.Error??"ยืนยันไม่สำเร็จ";return Redirect(Role=="Cashier"?"/cashier":"/admin"); }

    [HttpGet("artist-dashboard")]
    public async Task<IActionResult> ArtistDashboard() { if(!Allowed("Artist","Manager","Owner"))return Denied();var result=await api.Get<List<BookingDto>>("api/bookings");ViewBag.Error=result.Error;return View(result.Value??[]); }
    [HttpPost("artist/bookings/{id:int}/complete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteBooking(int id) { if(!Allowed("Artist","Manager","Owner"))return Denied();var result=await api.Send("POST",$"api/bookings/{id}/complete",new{});TempData[result.Ok?"Success":"Error"]=result.Ok?"ปิดงานเรียบร้อย":result.Error??"เปลี่ยนสถานะไม่สำเร็จ";return RedirectToAction(nameof(ArtistDashboard)); }
    [HttpGet("it")]
    public async Task<IActionResult> It() { if(!Allowed("IT Staff","Manager","Owner"))return Denied();var result=await api.Get<JsonElement>("api/it/overview");ViewBag.Error=result.Error;return View(result.Value.ToString()); }

    private string Role=>HttpContext.Session.GetString("Cafe.Role")??"";
    private bool Allowed(params string[] roles)=>!string.IsNullOrEmpty(HttpContext.Session.GetString("Cafe.Token"))&&roles.Contains(Role,StringComparer.OrdinalIgnoreCase);
    private IActionResult Denied()=>RedirectToAction("Login","Account",new{returnUrl=Request.Path+Request.QueryString});
}
