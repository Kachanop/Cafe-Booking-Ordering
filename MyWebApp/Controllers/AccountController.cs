using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.Services;

namespace MyWebApp.Controllers;

public sealed class AccountController(CafeBackendClient api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginForm { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var result = await api.Send<AuthResult>(HttpMethod.Post, "api/auth/login", new { form.Email, form.Password });
        if (result.Value is null) { ModelState.AddModelError(string.Empty, result.Error ?? "อีเมลหรือรหัสผ่านไม่ถูกต้อง"); return View(form); }
        SaveSession(result.Value);
        if (!string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl)) return Redirect(form.ReturnUrl);
        return Redirect(result.Value.User.Role switch { "Owner" or "Manager" => "/admin", "Cashier" => "/cashier", "Kitchen" => "/kds", "Artist" => "/artist-dashboard", "Waiter" => "/tables", "IT Staff" => "/it", _ => "/" });
    }

    [HttpGet]
    public IActionResult Signup() => View(new RegisterForm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(RegisterForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var result = await api.Send<AuthResult>(HttpMethod.Post, "api/auth/register", new { form.Email, form.Password, form.FullName });
        if (result.Value is null) { ModelState.AddModelError(string.Empty, result.Error ?? "สมัครสมาชิกไม่สำเร็จ"); return View(form); }
        SaveSession(result.Value); return RedirectToAction("Profile");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!IsSignedIn) return RedirectToAction("Login", new { returnUrl = "/account/profile" });
        var profile = await api.Get<ProfileDto>("api/profile");
        if (profile.Value is null) { ClearSession(); return RedirectToAction("Login"); }
        return View(new ProfileForm { FullName = profile.Value.FullName, Email = profile.Value.Email, Phone = profile.Value.Phone, Role = profile.Value.Role });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileForm form)
    {
        if (!IsSignedIn) return RedirectToAction("Login");
        if (!ModelState.IsValid) return View(form);
        var result = await api.Send("PUT", "api/profile", new { form.FullName, form.Phone });
        if (!result.Ok) { ModelState.AddModelError(string.Empty, result.Error ?? "บันทึกข้อมูลไม่สำเร็จ"); return View(form); }
        HttpContext.Session.SetString("Cafe.Name", form.FullName.Trim()); TempData["Success"] = "บันทึกข้อมูลบัญชีแล้ว"; return RedirectToAction("Profile");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (IsSignedIn) await api.Send("POST", "api/auth/logout");
        ClearSession(); return RedirectToAction("Index", "Home");
    }

    private bool IsSignedIn => !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("Cafe.Token"));
    private void SaveSession(AuthResult auth)
    {
        HttpContext.Session.SetString("Cafe.Token", auth.AccessToken); HttpContext.Session.SetString("Cafe.Name", auth.User.FullName);
        HttpContext.Session.SetString("Cafe.Role", auth.User.Role ?? "Member"); HttpContext.Session.SetString("Cafe.Email", auth.User.Email ?? ""); HttpContext.Session.SetInt32("Cafe.UserId", auth.User.Id);
    }
    private void ClearSession() { HttpContext.Session.Clear(); }
}
