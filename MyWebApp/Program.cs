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

// WAL allows screens to read while another request is writing. A longer SQLite
// busy timeout lets concurrent cashier/booking requests wait for the active writer.
var connectionString = $"Data Source={databasePath};Foreign Keys=True;Default Timeout=30;Pooling=True";
builder.Services.AddDbContextFactory<CafeDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IPasswordHasher<CafeUser>, PasswordHasher<CafeUser>>();
builder.Services.AddScoped<CafeDataService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<CafeDataService>().InitializeAsync();

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
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
        // Avoid marking the whole session as modified on every request. Multiple
        // tabs commonly load pages at once; unnecessary session commits can race.
        else if (!string.Equals(context.Session.GetString("Cafe.Role"), role, StringComparison.Ordinal))
            context.Session.SetString("Cafe.Role", role);
    }
    await next();
});
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
