using cafe_system_csharp.Components;
using CafeSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<CafeDbContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("Cafe") ?? Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(connection)) options.UseNpgsql(ToNpgsqlConnectionString(connection));
});
builder.Services.AddHttpClient("Supabase", client =>
{
    var url = builder.Configuration["Supabase:Url"] ?? Environment.GetEnvironmentVariable("SUPABASE_URL") ?? Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_URL");
    if (Uri.TryCreate(url, UriKind.Absolute, out var baseUri)) client.BaseAddress = baseUri;
});
builder.Services.AddAuthentication("CafeCookie").AddCookie("CafeCookie", options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/unauthorized";
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/auth/login", async (HttpContext context, IHttpClientFactory clients, IConfiguration configuration, IDbContextFactory<CafeDbContext> dbFactory) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var url = configuration["Supabase:Url"] ?? Environment.GetEnvironmentVariable("SUPABASE_URL") ?? Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_URL");
    var key = configuration["Supabase:PublishableKey"] ?? Environment.GetEnvironmentVariable("SUPABASE_PUBLISHABLE_KEY") ?? Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY") ?? Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_ANON_KEY");
    if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key)) return Results.Redirect("/login?error=ตั้งค่าการเข้าสู่ระบบไม่ครบ");
    var client = clients.CreateClient("Supabase");
    using var request = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/auth/v1/token?grant_type=password")
    {
        Content = System.Net.Http.Json.JsonContent.Create(new { email, password })
    };
    request.Headers.Add("apikey", key);
    using var response = await client.SendAsync(request);
    if (!response.IsSuccessStatusCode) return Results.Redirect("/login?error=อีเมลหรือรหัสผ่านไม่ถูกต้อง");
    var auth = await response.Content.ReadFromJsonAsync<SupabaseSession>();
    if (auth is null) return Results.Redirect("/login?error=เข้าสู่ระบบไม่สำเร็จ");
    await using var db = await dbFactory.CreateDbContextAsync();
    var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.AuthUserId == auth.User.Id);
    if (user is null) return Results.Redirect("/login?error=ไม่พบบัญชีในระบบร้าน");
    var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.FullName), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role.Name) };
    await context.SignInAsync("CafeCookie", new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "CafeCookie")));
    return Results.Redirect("/");
});
app.MapPost("/auth/signup", async (HttpContext context, IHttpClientFactory clients, IConfiguration configuration, IDbContextFactory<CafeDbContext> dbFactory) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString().Trim().ToLowerInvariant();
    var password = form["password"].ToString();
    var fullName = form["fullName"].ToString().Trim();
    if (string.IsNullOrWhiteSpace(email) || password.Length < 8 || string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100) return Results.Redirect("/signup?error=กรอกข้อมูลให้ครบและใช้รหัสผ่านอย่างน้อย%208%20ตัวอักษร");
    var url = configuration["Supabase:Url"] ?? Environment.GetEnvironmentVariable("SUPABASE_URL") ?? Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_URL");
    var key = configuration["Supabase:PublishableKey"] ?? Environment.GetEnvironmentVariable("SUPABASE_PUBLISHABLE_KEY") ?? Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY") ?? Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_ANON_KEY");
    if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key)) return Results.Redirect("/signup?error=ตั้งค่าการสมัครสมาชิกไม่ครบ");
    var request = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/auth/v1/signup") { Content = System.Net.Http.Json.JsonContent.Create(new { email, password, data = new { full_name = fullName, role = "member" } }) };
    request.Headers.Add("apikey", key);
    using var response = await clients.CreateClient("Supabase").SendAsync(request);
    if (!response.IsSuccessStatusCode) return Results.Redirect("/signup?error=สมัครสมาชิกไม่สำเร็จ%20กรุณาตรวจสอบอีเมล");
    using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    if (!payload.RootElement.TryGetProperty("id", out var idElement) || !Guid.TryParse(idElement.GetString(), out var authId)) return Results.Redirect("/login?message=สมัครสมาชิกแล้ว%20กรุณายืนยันอีเมล");
    await using var db = await dbFactory.CreateDbContextAsync();
    if (!await db.Users.AnyAsync(x => x.AuthUserId == authId))
    {
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == "Member");
        if (role is null) { role = new CafeSystem.Models.Role { Name = "Member" }; db.Roles.Add(role); await db.SaveChangesAsync(); }
        db.Users.Add(new CafeSystem.Models.CafeUser { AuthUserId = authId, RoleId = role.Id, Email = email, FullName = fullName, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
    return Results.Redirect("/login?message=สมัครสมาชิกแล้ว%20กรุณาเข้าสู่ระบบ");
});
app.MapPost("/auth/logout", async (HttpContext context) => { await context.SignOutAsync("CafeCookie"); return Results.Redirect("/"); });

app.Run();

static string ToNpgsqlConnectionString(string value)
{
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "postgres" && uri.Scheme != "postgresql")) return value;
    var credentials = uri.UserInfo.Split(':', 2);
    var builder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : "",
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = Npgsql.SslMode.Require
    };
    return builder.ConnectionString;
}

record SupabaseSession(SupabaseUser User);
record SupabaseUser(Guid Id);
