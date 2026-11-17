using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers;

public sealed class AccountController(CafeDataService cafe) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginForm { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var (user, error) = await cafe.LoginAsync(form);
        if (user is null) { ModelState.AddModelError(string.Empty, error ?? "อีเมลหรือรหัสผ่านไม่ถูกต้อง"); return View(form); }
        SaveSession(user);
        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl)) return Redirect(form.ReturnUrl);
        return Redirect(user.Role.Name switch { "Owner" or "Manager" => "/admin", "Cashier" => "/cashier", "Kitchen" => "/kds", "Artist" => "/artist-dashboard", "Waiter" => "/tables", "IT Staff" => "/it", _ => "/" });
    }

    [HttpGet]
    public IActionResult Signup() => View(new RegisterForm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(RegisterForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var (user, error) = await cafe.RegisterAsync(form);
        if (user is null) { ModelState.AddModelError(string.Empty, error ?? "สมัครสมาชิกไม่สำเร็จ"); return View(form); }
        SaveSession(user);
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetInt32("Cafe.UserId");
        if (userId is null) return RedirectToAction(nameof(Login), new { returnUrl = "/Account/Profile" });
        var profile = await cafe.GetProfileAsync(userId.Value);
        if (profile is null) { HttpContext.Session.Clear(); return RedirectToAction(nameof(Login)); }
        return View(new ProfileForm { FullName = profile.FullName, Email = profile.Email, Phone = profile.Phone, Role = profile.Role });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileForm form)
    {
        var userId = HttpContext.Session.GetInt32("Cafe.UserId");
        if (userId is null) return RedirectToAction(nameof(Login));
        if (!ModelState.IsValid) return View(form);
        if (!await cafe.UpdateProfileAsync(userId.Value, form)) { HttpContext.Session.Clear(); return RedirectToAction(nameof(Login)); }
        HttpContext.Session.SetString("Cafe.Name", form.FullName.Trim());
        TempData["Success"] = "บันทึกข้อมูลบัญชีแล้ว";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private void SaveSession(CafeUser user)
    {
        HttpContext.Session.SetInt32("Cafe.UserId", user.Id);
        HttpContext.Session.SetString("Cafe.Name", user.FullName);
        HttpContext.Session.SetString("Cafe.Role", user.Role.Name);
        HttpContext.Session.SetString("Cafe.Email", user.Email ?? "");
    }
}
