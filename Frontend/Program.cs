using CafeSystem.Frontend.Services;
using CafeFrontend.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddScoped<FrontendSession>();
builder.Services.AddHttpClient<CafeApi>(client => client.BaseAddress = new Uri(builder.Configuration["BackendUrl"] ?? "http://localhost:5080/"));
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
