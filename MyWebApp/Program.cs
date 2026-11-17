using MyWebApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => { options.IdleTimeout = TimeSpan.FromHours(12); options.Cookie.Name = ".Cafe.Session"; options.Cookie.HttpOnly = true; options.Cookie.IsEssential = true; options.Cookie.SameSite = SameSiteMode.Lax; });
builder.Services.AddHttpClient<CafeBackendClient>(client => { client.BaseAddress = new Uri(builder.Configuration["BackendUrl"] ?? "http://localhost:5080/"); client.Timeout = TimeSpan.FromSeconds(12); });

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
