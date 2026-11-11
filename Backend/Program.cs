using System.Security.Cryptography;
using System.Text;
using CafeSystem.Data;
using CafeSystem.Models;
using CafeSystem.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataPath);
builder.Services.AddDbContextFactory<CafeDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(dataPath, "cafe.db")}"));
builder.Services.AddSingleton<IPasswordHasher<CafeUser>, PasswordHasher<CafeUser>>();
builder.Services.AddAuthentication("CafeBearer").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TokenAuthHandler>("CafeBearer", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy.WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:5002").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

await InitializeDatabase(app.Services);

app.MapGet("/health", () => Results.Ok(new { status = "ok", database = "sqlite" }));
app.MapGet("/api/menu", async (IDbContextFactory<CafeDbContext> factory) =>
{
    await using var db = await factory.CreateDbContextAsync();
    return await (from item in db.MenuItems where item.IsAvailable join category in db.MenuCategories on item.CategoryId equals category.Id orderby category.Id, item.Id select new MenuDto(item.Id, category.Name, item.Name, item.Price)).ToListAsync();
});
app.MapGet("/api/services", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await db.Services.OrderBy(x => x.Id).Select(x => new ServiceDto(x.Id, x.Name, x.Price)).ToListAsync(); });
app.MapGet("/api/artists", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await (from u in db.Users join r in db.Roles on u.RoleId equals r.Id join p in db.StaffProfiles on u.Id equals p.UserId where r.Name == "Artist" select new ArtistDto(u.Id, u.FullName, p.Bio, p.PortfolioUrl, p.Specialties)).ToListAsync(); });
app.MapGet("/api/it/overview", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return Results.Ok(new { users=await db.Users.CountAsync(), services=await db.Services.CountAsync(), menuItems=await db.MenuItems.CountAsync(), tables=await db.Tables.CountAsync(), bookings=await db.Bookings.CountAsync(), orders=await db.Orders.CountAsync() }); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "IT Staff,Manager,Owner" });

app.MapPost("/api/auth/register", async (RegisterRequest request, IDbContextFactory<CafeDbContext> factory, IPasswordHasher<CafeUser> hasher) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var name = request.FullName.Trim();
    if (name.Length is < 2 or > 100 || request.Password.Length < 8 || !email.Contains('@')) return Results.BadRequest(new { error = "กรอกชื่อ อีเมล และรหัสผ่านอย่างน้อย 8 ตัวอักษร" });
    await using var db = await factory.CreateDbContextAsync();
    if (await db.Users.AnyAsync(x => x.Email == email)) return Results.Conflict(new { error = "อีเมลนี้มีบัญชีแล้ว" });
    var role = await db.Roles.SingleAsync(x => x.Name == "Member");
    var user = new CafeUser { Email = email, FullName = name, RoleId = role.Id, Role = role, CreatedAt = DateTime.UtcNow };
    user.PasswordHash = hasher.HashPassword(user, request.Password);
    db.Users.Add(user); await db.SaveChangesAsync();
    return Results.Ok(await CreateToken(db, user));
});
app.MapPost("/api/auth/login", async (LoginRequest request, IDbContextFactory<CafeDbContext> factory, IPasswordHasher<CafeUser> hasher) =>
{
    await using var db = await factory.CreateDbContextAsync();
    var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLowerInvariant());
    if (user?.PasswordHash is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed) return Results.Unauthorized();
    return Results.Ok(await CreateToken(db, user));
});

var secure = app.MapGroup("/api").RequireAuthorization();
secure.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) => Results.Ok(new { id = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, name = user.Identity?.Name, role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value }));
secure.MapGet("/bookings", async (System.Security.Claims.ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) =>
{
    await using var db = await factory.CreateDbContextAsync();
    var userId = int.Parse(actor.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var role = actor.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;
    var query = db.Bookings.AsQueryable();
    if (role == "Member") query = query.Where(x => x.CustomerId == userId);
    else if (role == "Artist") query = query.Where(x => x.StaffId == userId);
    return await query.OrderBy(x => x.BookingDate).Select(x => new BookingDto(x.Id, x.CustomerId, x.StaffId, x.ServiceId, x.BookingDate, x.TimeSlot, x.Status, x.TotalPrice)).ToListAsync();
});
secure.MapPost("/bookings", async (CreateBookingRequest request, System.Security.Claims.ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) =>
{
    var role = actor.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;
    if (role is not ("Member" or "Artist" or "Waiter" or "Manager" or "Owner")) return Results.Forbid();
    var slots = new[] { "10:00-12:00", "13:00-15:00", "15:00-17:00", "17:00-19:00" };
    if (!slots.Contains(request.TimeSlot) || request.BookingDate.Date < DateTime.UtcNow.Date) return Results.BadRequest(new { error = "วันที่หรือรอบเวลาไม่ถูกต้อง" });
    await using var db = await factory.CreateDbContextAsync();
    var artist = await db.Users.Join(db.Roles, u => u.RoleId, r => r.Id, (u, r) => new { User = u, Role = r }).AnyAsync(x => x.User.Id == request.StaffId && x.Role.Name == "Artist" && db.StaffProfiles.Any(p => p.UserId == x.User.Id));
    var service = await db.Services.FindAsync(request.ServiceId);
    if (!artist || service is null) return Results.BadRequest(new { error = "ไม่พบบริการหรือช่าง" });
    var customerId = int.Parse(actor.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var booking = new Booking { CustomerId = customerId, StaffId = request.StaffId, ServiceId = service.Id, BookingDate = request.BookingDate.Date, TimeSlot = request.TimeSlot, TotalPrice = service.Price, CustomerNote = request.Note?.Trim()[..Math.Min(request.Note.Trim().Length, 500)], Status = "Pending", CreatedAt = DateTime.UtcNow };
    db.Bookings.Add(booking);
    try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Results.Conflict(new { error = "รอบเวลานี้ถูกจองแล้ว" }); }
    return Results.Created($"/api/bookings/{booking.Id}", booking);
});
secure.MapPost("/bookings/{id:int}/cancel", async (int id, System.Security.Claims.ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) =>
{
    await using var db = await factory.CreateDbContextAsync();
    var userId = int.Parse(actor.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == userId && x.Status == "Pending");
    if (booking is null) return Results.NotFound(); booking.Status = "Cancelled"; await db.SaveChangesAsync(); return Results.NoContent();
});
secure.MapGet("/tables", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await db.Tables.OrderBy(x => x.Id).Select(x => new TableDto(x.Id, x.TableNumber, x.CurrentSession != null, x.BillRequestedAt)).ToListAsync(); });
secure.MapPost("/tables/{id:int}/open", async (int id, System.Security.Claims.ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) =>
{
    if (actor.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value is not ("Waiter" or "Cashier" or "Manager" or "Owner")) return Results.Forbid();
    await using var db = await factory.CreateDbContextAsync(); var table = await db.Tables.FindAsync(id); if (table is null || table.CurrentSession is not null) return Results.Conflict(new { error = "โต๊ะไม่ว่างหรือไม่พบโต๊ะ" }); table.CurrentSession = Guid.NewGuid().ToString("N"); table.OpenedAt = DateTime.UtcNow; await db.SaveChangesAsync(); return Results.Ok(new { table.Id, session = table.CurrentSession });
});
secure.MapPost("/tables/{id:int}/orders", async (int id, OrderRequest request, IDbContextFactory<CafeDbContext> factory) =>
{
    if (request.Items.Count is < 1 or > 30 || request.Items.Any(x => x.Quantity is < 1 or > 20)) return Results.BadRequest(new { error = "รายการสั่งซื้อไม่ถูกต้อง" });
    await using var db = await factory.CreateDbContextAsync(); var table = await db.Tables.FindAsync(id); if (table?.CurrentSession != request.Session) return Results.Conflict(new { error = "QR Code หมดอายุ" });
    var ids = request.Items.Select(x => x.MenuItemId).Distinct().ToArray(); var menu = await db.MenuItems.Where(x => ids.Contains(x.Id) && x.IsAvailable).ToDictionaryAsync(x => x.Id); if (menu.Count != ids.Length) return Results.BadRequest(new { error = "มีเมนูที่ไม่พร้อมจำหน่าย" });
    var order = await db.Orders.Where(x => x.TableId == id && (x.Status == "Dining" || x.Status == "Billing")).OrderByDescending(x => x.Id).FirstOrDefaultAsync(); if (order is null) { order = new CafeOrder { TableId = id, Status = "Dining", CreatedAt = DateTime.UtcNow }; db.Orders.Add(order); await db.SaveChangesAsync(); }
    foreach (var line in request.Items) db.OrderItems.Add(new OrderItem { OrderId = order.Id, MenuItemId = line.MenuItemId, Quantity = line.Quantity, Note = line.Note?.Trim()[..Math.Min(line.Note.Trim().Length, 180)], KdsStatus = "Pending" });
    order.Status = "Dining"; table.BillRequestedAt = null; await db.SaveChangesAsync(); order.TotalAmount = await (from line in db.OrderItems join item in db.MenuItems on line.MenuItemId equals item.Id where line.OrderId == order.Id select item.Price * line.Quantity).SumAsync(); await db.SaveChangesAsync(); return Results.Ok(new { order.Id, order.TotalAmount });
});
secure.MapPost("/tables/{id:int}/bill", async (int id, BillRequest request, IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); var table = await db.Tables.FindAsync(id); if (table?.CurrentSession != request.Session) return Results.Conflict(); var orders = await db.Orders.Where(x => x.TableId == id && x.Status == "Dining").ToListAsync(); if (orders.Count == 0) return Results.BadRequest(new { error = "โต๊ะนี้ยังไม่มีรายการ" }); foreach (var order in orders) order.Status = "Billing"; table.BillRequestedAt = DateTime.UtcNow; await db.SaveChangesAsync(); return Results.NoContent(); });
secure.MapGet("/kds", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await (from line in db.OrderItems join order in db.Orders on line.OrderId equals order.Id join table in db.Tables on order.TableId equals table.Id join menu in db.MenuItems on line.MenuItemId equals menu.Id where order.Status != "Paid" orderby order.CreatedAt select new { line.Id, table = table.TableNumber, menu = menu.Name, line.Quantity, line.KdsStatus, line.Note, order.CreatedAt }).ToListAsync(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Kitchen,Manager,Owner" });
secure.MapPost("/kds/{id:int}/status", async (int id, KitchenStatusRequest request, IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); var line = await db.OrderItems.FindAsync(id); if (line is null) return Results.NotFound(); var next = line.KdsStatus == "Pending" ? "Cooking" : line.KdsStatus == "Cooking" ? "Served" : null; if (next != request.Status) return Results.Conflict(new { error = "เปลี่ยนสถานะได้ครั้งละหนึ่งขั้น" }); line.KdsStatus = next; await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Kitchen,Manager,Owner" });
secure.MapGet("/cashier", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await (from t in db.Tables join o in db.Orders.Where(x => x.Status == "Billing") on t.Id equals o.TableId select new { tableId = t.Id, t.TableNumber, t.BillRequestedAt, orderId = o.Id, o.TotalAmount }).ToListAsync(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Cashier,Manager,Owner" });
secure.MapPost("/tables/{id:int}/close", async (int id, PaymentRequest payment, IDbContextFactory<CafeDbContext> factory) => { if (payment.Method is not ("CASH" or "PROMPTPAY")) return Results.BadRequest(new { error = "ช่องทางชำระเงินไม่ถูกต้อง" }); await using var db = await factory.CreateDbContextAsync(); var table = await db.Tables.FindAsync(id); var order = await db.Orders.Where(x => x.TableId == id && x.Status == "Billing").OrderByDescending(x => x.Id).FirstOrDefaultAsync(); if (table?.CurrentSession is null || order is null) return Results.Conflict(new { error = "ไม่พบบิลที่รอชำระ" }); if (await db.OrderItems.AnyAsync(x => x.OrderId == order.Id && x.KdsStatus != "Served")) return Results.Conflict(new { error = "ยังมีรายการที่ครัวทำไม่เสร็จ" }); order.Status = "Paid"; table.CurrentSession = null; table.OpenedAt = null; table.BillRequestedAt = null; db.Payments.Add(new Payment { ReferenceId = order.Id, ReferenceType = "ORDER", Amount = order.TotalAmount, Method = payment.Method, Status = "Success", PaidAt = DateTime.UtcNow }); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Cashier,Manager,Owner" });
secure.MapPost("/bookings/{id:int}/paid", async (int id, PaymentRequest payment, IDbContextFactory<CafeDbContext> factory) => { if (payment.Method is not ("CASH" or "PROMPTPAY")) return Results.BadRequest(new { error = "ช่องทางชำระเงินไม่ถูกต้อง" }); await using var db = await factory.CreateDbContextAsync(); var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Pending"); if (booking is null) return Results.Conflict(new { error = "ไม่พบรายการค้างชำระ" }); booking.Status = "Paid"; db.Payments.Add(new Payment { ReferenceId = id, ReferenceType = "BOOKING", Amount = booking.TotalPrice, Method = payment.Method, Status = "Success", PaidAt = DateTime.UtcNow }); await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Cashier,Manager,Owner" });
secure.MapGet("/admin/overview", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); var today=DateTime.UtcNow.Date; return Results.Ok(new { users=await db.Users.CountAsync(), bookingsToday=await db.Bookings.CountAsync(x=>x.BookingDate>=today), openTables=await db.Tables.CountAsync(x=>x.CurrentSession!=null), pendingBookings=await db.Bookings.CountAsync(x=>x.Status=="Pending"), paidToday=await db.Payments.Where(x=>x.Status=="Success"&&x.PaidAt>=today).SumAsync(x=>(decimal?)x.Amount)??0 }); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Manager,Owner" });

app.Run();

static async Task InitializeDatabase(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope(); var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CafeDbContext>>(); await using var db = await factory.CreateDbContextAsync(); await db.Database.EnsureCreatedAsync();
    foreach (var name in new[] { "Guest", "Member", "Artist", "Waiter", "Kitchen", "Cashier", "Manager", "Owner", "IT Staff" }) if (!await db.Roles.AnyAsync(x => x.Name == name)) db.Roles.Add(new Role { Name = name });
    await db.SaveChangesAsync();
    if (!await db.MenuCategories.AnyAsync()) { var drink = new MenuCategory { Name = "Coffee & Drinks" }; var bakery = new MenuCategory { Name = "Bakery" }; db.MenuCategories.AddRange(drink, bakery); await db.SaveChangesAsync(); db.MenuItems.AddRange(new MenuItem { CategoryId = drink.Id, Name = "Americano", Price = 80 }, new MenuItem { CategoryId = drink.Id, Name = "Café Latte", Price = 95 }, new MenuItem { CategoryId = bakery.Id, Name = "Butter Croissant", Price = 75 }); }
    if (!await db.Services.AnyAsync()) db.Services.AddRange(new Service { Name = "Photography", Price = 1800 }, new Service { Name = "Makeup", Price = 1200 });
    if (!await db.Tables.AnyAsync()) db.Tables.AddRange(Enumerable.Range(1, 8).Select(i => new TableCafe { TableNumber = $"T{i:00}" }));
    if (!await db.StaffProfiles.AnyAsync())
    {
        var artistRole = await db.Roles.SingleAsync(x => x.Name == "Artist");
        var artist = new CafeUser { FullName = "Studio Artist", RoleId = artistRole.Id, CreatedAt = DateTime.UtcNow };
        db.Users.Add(artist); await db.SaveChangesAsync(); db.StaffProfiles.Add(new StaffProfile { UserId = artist.Id, Bio = "ช่างภาพและช่างแต่งหน้าประจำสตูดิโอ", Specialties = ["Photography", "Makeup"] });
    }
    await db.SaveChangesAsync();
}

static async Task<object> CreateToken(CafeDbContext db, CafeUser user)
{
    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    db.LoginSessions.Add(new LoginSession { TokenHash = hash, UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(14) }); await db.SaveChangesAsync();
    return new { accessToken = token, expiresInDays = 14, user = new { user.Id, user.FullName, user.Email, role = user.Role?.Name } };
}

record RegisterRequest(string Email, string Password, string FullName);
record LoginRequest(string Email, string Password);
record MenuDto(int Id, string Category, string Name, decimal Price);
record ServiceDto(int Id, string Name, decimal Price);
record ArtistDto(int Id, string FullName, string? Bio, string? PortfolioUrl, string[] Specialties);
record BookingDto(int Id, int CustomerId, int StaffId, int ServiceId, DateTime BookingDate, string TimeSlot, string Status, decimal TotalPrice);
record CreateBookingRequest(int StaffId, int ServiceId, DateTime BookingDate, string TimeSlot, string? Note);
record TableDto(int Id, string TableNumber, bool IsOpen, DateTime? BillRequestedAt);
record OrderRequest(string Session, List<OrderLineRequest> Items);
record OrderLineRequest(int MenuItemId, int Quantity, string? Note);
record BillRequest(string Session);
record KitchenStatusRequest(string Status);
record PaymentRequest(string Method);
