using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.Name = ".Cafe.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var databasePath = builder.Configuration["Database:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "Data", "cafe.db");
if (!Path.IsPathRooted(databasePath)) databasePath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, databasePath));
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
builder.Services.AddDbContextFactory<CafeDbContext>(options => options.UseSqlite($"Data Source={databasePath};Foreign Keys=True;Default Timeout=15"));
builder.Services.AddScoped<IPasswordHasher<CafeUser>, PasswordHasher<CafeUser>>();
builder.Services.AddScoped<CafeDataService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<CafeDataService>().InitializeAsync();

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.Use(async (context, next) =>
{
    var userId = context.Session.GetInt32("Cafe.UserId");
    if (userId is int id)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var role = await scope.ServiceProvider.GetRequiredService<CafeDataService>().GetRoleAsync(id);
        if (role is null) context.Session.Clear();
        else context.Session.SetString("Cafe.Role", role);
    }
    await next();
});
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
