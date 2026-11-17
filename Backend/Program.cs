using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using CafeSystem.Data;
using CafeSystem.Models;
using CafeSystem.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dataPath = builder.Configuration["Database:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "Data", "cafe.db");
if (!Path.IsPathRooted(dataPath)) dataPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, dataPath));
Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!);
builder.Services.AddDbContextFactory<CafeDbContext>(options => options.UseSqlite($"Data Source={dataPath};Foreign Keys=True;Default Timeout=15"));
builder.Services.AddSingleton<IPasswordHasher<CafeUser>, PasswordHasher<CafeUser>>();
builder.Services.AddAuthentication("CafeBearer").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TokenAuthHandler>("CafeBearer", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy.WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:5002").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
await InitializeDatabase(app.Services, builder.Configuration);

app.MapGet("/health", async (IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.ExecuteSqlRawAsync("SELECT 1");
    return Results.Ok(new { status = "ok", database = "sqlite" });
});
app.MapGet("/api/menu", async (IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync();
    return await (from item in db.MenuItems where item.IsAvailable join category in db.MenuCategories on item.CategoryId equals category.Id orderby category.Id, item.Id select new MenuDto(item.Id, category.Name, item.Name, item.Price, item.IsAvailable)).ToListAsync();
});
app.MapGet("/api/services", async (IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync();
    return await db.Services.OrderBy(x => x.Id).Select(x => new ServiceDto(x.Id, x.Name, x.Price)).ToListAsync();
});
app.MapGet("/api/artists", async (IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync();
    return await (from u in db.Users join r in db.Roles on u.RoleId equals r.Id join p in db.StaffProfiles on u.Id equals p.UserId where r.Name == "Artist" select new ArtistDto(u.Id, u.FullName, p.Bio, p.PortfolioUrl, p.Specialties)).ToListAsync();
});
app.MapGet("/api/availability", async (DateTime date, int staffId, IDbContextFactory<CafeDbContext> factory) => {
    if (date.Date < BangkokToday()) return Results.BadRequest(new { error = "เลือกวันที่ตั้งแต่วันนี้เป็นต้นไป" });
    await using var db = await factory.CreateDbContextAsync();
    var taken = await db.Bookings.Where(x => x.StaffId == staffId && x.BookingDate == date.Date && x.Status != "Cancelled").Select(x => x.TimeSlot).ToListAsync();
    return Results.Ok(BookingSlots.All.Select(slot => new SlotAvailabilityDto(slot, !taken.Contains(slot))));
});

app.MapPost("/api/auth/register", async (RegisterRequest request, IDbContextFactory<CafeDbContext> factory, IPasswordHasher<CafeUser> hasher) => {
    var email = request.Email?.Trim().ToLowerInvariant() ?? "";
    var name = request.FullName?.Trim() ?? "";
    if (name.Length is < 2 or > 100 || request.Password is null || request.Password.Length < 8 || !email.Contains('@')) return Results.BadRequest(new { error = "กรอกชื่อ อีเมล และรหัสผ่านอย่างน้อย 8 ตัวอักษร" });
    await using var db = await factory.CreateDbContextAsync();
    if (await db.Users.AnyAsync(x => x.Email == email)) return Results.Conflict(new { error = "อีเมลนี้มีบัญชีแล้ว" });
    var role = await db.Roles.SingleAsync(x => x.Name == "Member");
    var user = new CafeUser { Email = email, FullName = name, RoleId = role.Id, Role = role, CreatedAt = DateTime.UtcNow };
    user.PasswordHash = hasher.HashPassword(user, request.Password);
    db.Users.Add(user); await db.SaveChangesAsync();
    return Results.Ok(await CreateToken(db, user));
});
app.MapPost("/api/auth/login", async (LoginRequest request, IDbContextFactory<CafeDbContext> factory, IPasswordHasher<CafeUser> hasher) => {
    await using var db = await factory.CreateDbContextAsync();
    var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLowerInvariant());
    if (user?.PasswordHash is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed) return Results.Unauthorized();
    return Results.Ok(await CreateToken(db, user));
});

// Table ordering is public only when the caller proves possession of the active table-session token.
app.MapGet("/api/tables/{id:int}/session", async (int id, string session, IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync();
    var table = await db.Tables.SingleOrDefaultAsync(x => x.Id == id && x.CurrentSession == session);
    if (table is null) return Results.Conflict(new { error = "QR session หมดอายุ กรุณาเรียกพนักงาน" });
    var orders = await db.Orders.Where(x => x.TableId == id && (x.Status == "Dining" || x.Status == "Billing")).OrderBy(x => x.Id).ToListAsync();
    var ids = orders.Select(x => x.Id).ToArray();
    var lines = await (from line in db.OrderItems join menu in db.MenuItems on line.MenuItemId equals menu.Id where ids.Contains(line.OrderId) select new { line.OrderId, line.Id, menu.Name, line.Quantity, Price = line.UnitPrice, line.Note, line.KdsStatus }).ToListAsync();
    var menuItems = await (from item in db.MenuItems where item.IsAvailable join category in db.MenuCategories on item.CategoryId equals category.Id orderby category.Id, item.Id select new MenuDto(item.Id, category.Name, item.Name, item.Price, item.IsAvailable)).ToListAsync();
    return Results.Ok(new TableSessionDto(table.Id, table.TableNumber, table.BillRequestedAt is not null, menuItems, orders.Select(order => new TableOrderDto(order.Id, order.Status, order.TotalAmount, lines.Where(line => line.OrderId == order.Id).Select(line => new TableOrderLineDto(line.Id, line.Name, line.Quantity, line.Price, line.Note, line.KdsStatus)).ToList())).ToList()));
});
app.MapPost("/api/tables/{id:int}/orders", async (int id, OrderRequest request, IDbContextFactory<CafeDbContext> factory) => {
    if (request.Items is null || request.Items.Count is < 1 or > 30 || request.Items.Any(x => x.Quantity is < 1 or > 20 || (x.Note?.Length ?? 0) > 180)) return Results.BadRequest(new { error = "รายการสั่งซื้อไม่ถูกต้อง" });
    await using var db = await factory.CreateDbContextAsync();
    var table = await db.Tables.SingleOrDefaultAsync(x => x.Id == id && x.CurrentSession == request.Session);
    if (table is null) return Results.Conflict(new { error = "QR session หมดอายุ กรุณาเรียกพนักงาน" });
    var ids = request.Items.Select(x => x.MenuItemId).Distinct().ToArray();
    var menu = await db.MenuItems.Where(x => ids.Contains(x.Id) && x.IsAvailable).ToDictionaryAsync(x => x.Id);
    if (menu.Count != ids.Length) return Results.BadRequest(new { error = "มีเมนูที่ไม่พร้อมจำหน่าย" });
    var order = await db.Orders.Where(x => x.TableId == id && x.Status == "Dining").OrderByDescending(x => x.Id).FirstOrDefaultAsync();
    if (order is null) { order = new CafeOrder { TableId = id, Status = "Dining", CreatedAt = DateTime.UtcNow }; db.Orders.Add(order); await db.SaveChangesAsync(); }
    foreach (var line in request.Items) db.OrderItems.Add(new OrderItem { OrderId = order.Id, MenuItemId = line.MenuItemId, Quantity = line.Quantity, UnitPrice = menu[line.MenuItemId].Price, Note = line.Note?.Trim(), KdsStatus = "Pending" });
    table.BillRequestedAt = null;
    await db.SaveChangesAsync();
    var orderLines = await db.OrderItems.Where(line => line.OrderId == order.Id).Select(line => new { line.UnitPrice, line.Quantity }).ToListAsync();
    order.TotalAmount = orderLines.Sum(line => line.UnitPrice * line.Quantity);
    await db.SaveChangesAsync();
    return Results.Ok(new { order.Id, order.TotalAmount });
});
app.MapPost("/api/tables/{id:int}/bill", async (int id, BillRequest request, IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync();
    var table = await db.Tables.SingleOrDefaultAsync(x => x.Id == id && x.CurrentSession == request.Session);
    if (table is null) return Results.Conflict(new { error = "QR session หมดอายุ กรุณาเรียกพนักงาน" });
    var orders = await db.Orders.Where(x => x.TableId == id && x.Status == "Dining").ToListAsync();
    if (orders.Count == 0) return Results.BadRequest(new { error = "โต๊ะนี้ยังไม่มีรายการ" });
    foreach (var order in orders) order.Status = "Billing";
    table.BillRequestedAt = DateTime.UtcNow; await db.SaveChangesAsync(); return Results.NoContent();
});

var secure = app.MapGroup("/api").RequireAuthorization();
secure.MapGet("/me", (ClaimsPrincipal actor) => Results.Ok(new { id = actor.FindFirstValue(ClaimTypes.NameIdentifier), name = actor.Identity?.Name, role = actor.FindFirstValue(ClaimTypes.Role) }));
secure.MapPost("/auth/logout", async (ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) => {
    var userId = int.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var token = actor.FindFirstValue("session_hash");
    if (token is not null) { await using var db = await factory.CreateDbContextAsync(); var session = await db.LoginSessions.FindAsync(token); if (session is not null) { db.LoginSessions.Remove(session); await db.SaveChangesAsync(); } }
    return Results.NoContent();
});
secure.MapGet("/profile", async (ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) => {
    var id = int.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!); await using var db = await factory.CreateDbContextAsync();
    var user = await db.Users.Include(x => x.Role).SingleAsync(x => x.Id == id); return Results.Ok(new ProfileDto(user.Id, user.FullName, user.Email, user.Phone, user.Role.Name));
});
secure.MapPut("/profile", async (ProfileRequest request, ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) => {
    var id = int.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!); var name = request.FullName?.Trim() ?? "";
    if (name.Length is < 2 or > 100 || (request.Phone?.Length ?? 0) > 30) return Results.BadRequest(new { error = "ข้อมูลบัญชีไม่ถูกต้อง" });
    await using var db = await factory.CreateDbContextAsync(); var user = await db.Users.FindAsync(id); if (user is null) return Results.NotFound(); user.FullName = name; user.Phone = request.Phone?.Trim(); await db.SaveChangesAsync(); return Results.NoContent();
});
secure.MapGet("/bookings", async (ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync(); var userId = int.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!); var role = actor.FindFirstValue(ClaimTypes.Role)!;
    if (role is not ("Member" or "Artist" or "Waiter" or "Cashier" or "Manager" or "Owner")) return Results.Forbid();
    var query = db.Bookings.AsQueryable(); if (role == "Member") query = query.Where(x => x.CustomerId == userId); else if (role == "Artist") query = query.Where(x => x.StaffId == userId);
    return Results.Ok(await (from b in query join customer in db.Users on b.CustomerId equals customer.Id join staff in db.Users on b.StaffId equals staff.Id join service in db.Services on b.ServiceId equals service.Id orderby b.BookingDate, b.TimeSlot select new BookingDto(b.Id, b.CustomerId, customer.FullName, b.StaffId, staff.FullName, b.ServiceId, service.Name, b.BookingDate, b.TimeSlot, b.Status, b.TotalPrice, b.CustomerNote)).ToListAsync());
});
secure.MapPost("/bookings", async (CreateBookingRequest request, ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) => {
    var role = actor.FindFirstValue(ClaimTypes.Role)!; if (role is not ("Member" or "Artist" or "Waiter" or "Manager" or "Owner")) return Results.Forbid();
    if (!BookingSlots.All.Contains(request.TimeSlot) || request.BookingDate.Date < BangkokToday() || request.Note?.Length > 500) return Results.BadRequest(new { error = "วันที่หรือรอบเวลาไม่ถูกต้อง" });
    await using var db = await factory.CreateDbContextAsync();
    var artist = await (from u in db.Users join r in db.Roles on u.RoleId equals r.Id where u.Id == request.StaffId && r.Name == "Artist" join p in db.StaffProfiles on u.Id equals p.UserId select u.Id).AnyAsync();
    var service = await db.Services.FindAsync(request.ServiceId); if (!artist || service is null) return Results.BadRequest(new { error = "ไม่พบบริการหรือช่าง" });
    var customerId = int.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var booking = new Booking { CustomerId = customerId, StaffId = request.StaffId, ServiceId = service.Id, BookingDate = request.BookingDate.Date, TimeSlot = request.TimeSlot, TotalPrice = service.Price, CustomerNote = request.Note?.Trim(), Status = "Pending", CreatedAt = DateTime.UtcNow };
    db.Bookings.Add(booking); try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Results.Conflict(new { error = "รอบเวลานี้ถูกจองแล้ว" }); }
    return Results.Created($"/api/bookings/{booking.Id}", new { booking.Id, booking.Status, booking.TotalPrice });
});
secure.MapPost("/bookings/{id:int}/cancel", async (int id, ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync(); var userId = int.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!); var role = actor.FindFirstValue(ClaimTypes.Role);
    if (role is not ("Member" or "Artist" or "Waiter" or "Manager" or "Owner")) return Results.Forbid();
    var query = db.Bookings.Where(x => x.Id == id && x.Status == "Pending");
    if (role == "Member") query = query.Where(x => x.CustomerId == userId); else if (role == "Artist") query = query.Where(x => x.StaffId == userId);
    var booking = await query.SingleOrDefaultAsync(); if (booking is null) return Results.NotFound(); booking.Status = "Cancelled"; await db.SaveChangesAsync(); return Results.NoContent();
});
secure.MapGet("/tables", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await db.Tables.OrderBy(x => x.Id).Select(x => new TableDto(x.Id, x.TableNumber, x.CurrentSession != null, x.BillRequestedAt)).ToListAsync(); });
secure.MapPost("/tables/{id:int}/open", async (int id, ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) => {
    if (!InRoles(actor, "Waiter", "Cashier", "Manager", "Owner")) return Results.Forbid(); await using var db = await factory.CreateDbContextAsync(); var table = await db.Tables.FindAsync(id);
    if (table is null || table.CurrentSession is not null) return Results.Conflict(new { error = "โต๊ะไม่ว่างหรือไม่พบโต๊ะ" });
    table.CurrentSession = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(); table.OpenedAt = DateTime.UtcNow; await db.SaveChangesAsync();
    return Results.Ok(new { table.Id, table.TableNumber, session = table.CurrentSession, url = $"/table/{table.Id}?session={table.CurrentSession}" });
});
secure.MapGet("/kds", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await (from line in db.OrderItems join order in db.Orders on line.OrderId equals order.Id join table in db.Tables on order.TableId equals table.Id join menu in db.MenuItems on line.MenuItemId equals menu.Id where order.Status != "Paid" orderby order.CreatedAt select new KitchenItemDto(line.Id, order.Id, table.TableNumber, menu.Name, line.Quantity, line.KdsStatus, line.Note, order.CreatedAt)).ToListAsync(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Kitchen,Manager,Owner" });
secure.MapGet("/kds/summary", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await (from line in db.OrderItems join order in db.Orders on line.OrderId equals order.Id join menu in db.MenuItems on line.MenuItemId equals menu.Id where order.Status != "Paid" && line.KdsStatus != "Served" group line by menu.Name into g select new { menu = g.Key, quantity = g.Sum(x => x.Quantity) }).OrderBy(x => x.menu).ToListAsync(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Kitchen,Manager,Owner" });
secure.MapPost("/kds/{id:int}/status", async (int id, KitchenStatusRequest request, IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); var line = await db.OrderItems.FindAsync(id); if (line is null) return Results.NotFound(); var next = line.KdsStatus == "Pending" ? "Cooking" : line.KdsStatus == "Cooking" ? "Served" : null; if (next != request.Status) return Results.Conflict(new { error = "เปลี่ยนสถานะได้ครั้งละหนึ่งขั้น" }); line.KdsStatus = next; await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Kitchen,Manager,Owner" });
secure.MapGet("/cashier", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await (from t in db.Tables join o in db.Orders.Where(x => x.Status == "Billing") on t.Id equals o.TableId select new CashierRow(t.Id, t.TableNumber, t.BillRequestedAt, o.Id, o.TotalAmount, (from line in db.OrderItems join menu in db.MenuItems on line.MenuItemId equals menu.Id where line.OrderId == o.Id select new CashierLineDto(menu.Name, line.Quantity, line.UnitPrice)).ToList())).ToListAsync(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Cashier,Manager,Owner" });
secure.MapPost("/tables/{id:int}/close", async (int id, PaymentRequest payment, IDbContextFactory<CafeDbContext> factory) => {
    if (payment.Method is not ("CASH" or "PROMPTPAY")) return Results.BadRequest(new { error = "ช่องทางชำระเงินไม่ถูกต้อง" }); await using var db = await factory.CreateDbContextAsync();
    var table = await db.Tables.FindAsync(id); var orders = await db.Orders.Where(x => x.TableId == id && x.Status == "Billing").ToListAsync();
    if (table?.CurrentSession is null || orders.Count == 0) return Results.Conflict(new { error = "ไม่พบบิลที่รอชำระ" });
    if (await db.OrderItems.AnyAsync(x => orders.Select(o => o.Id).Contains(x.OrderId) && x.KdsStatus != "Served")) return Results.Conflict(new { error = "ยังมีรายการที่ครัวทำไม่เสร็จ" });
    foreach (var order in orders) { order.Status = "Paid"; db.Payments.Add(new Payment { ReferenceId = order.Id, ReferenceType = "ORDER", Amount = order.TotalAmount, Method = payment.Method, Status = "Success", PaidAt = DateTime.UtcNow }); }
    table.CurrentSession = null; table.OpenedAt = null; table.BillRequestedAt = null; await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Cashier,Manager,Owner" });
secure.MapPost("/bookings/{id:int}/paid", async (int id, PaymentRequest payment, IDbContextFactory<CafeDbContext> factory) => {
    if (payment.Method is not ("CASH" or "PROMPTPAY")) return Results.BadRequest(new { error = "ช่องทางชำระเงินไม่ถูกต้อง" }); await using var db = await factory.CreateDbContextAsync(); var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Pending");
    if (booking is null) return Results.Conflict(new { error = "ไม่พบรายการค้างชำระ" }); booking.Status = "Paid"; db.Payments.Add(new Payment { ReferenceId = id, ReferenceType = "BOOKING", Amount = booking.TotalPrice, Method = payment.Method, Status = "Success", PaidAt = DateTime.UtcNow }); await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Cashier,Manager,Owner" });
secure.MapPost("/bookings/{id:int}/complete", async (int id, IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Paid"); if (booking is null) return Results.Conflict(new { error = "ต้องเป็นรายการที่ชำระแล้ว" }); booking.Status = "Completed"; await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Artist,Manager,Owner" });

secure.MapGet("/admin/overview", async (IDbContextFactory<CafeDbContext> factory) => {
    await using var db = await factory.CreateDbContextAsync(); var today = BangkokToday(); var start = BangkokStartUtc(today); var end = start.AddDays(1);
    var cafePayments = await db.Payments.Where(x => x.Status == "Success" && x.ReferenceType == "ORDER" && x.PaidAt >= start && x.PaidAt < end).Select(x => x.Amount).ToListAsync();
    var studioPayments = await db.Payments.Where(x => x.Status == "Success" && x.ReferenceType == "BOOKING" && x.PaidAt >= start && x.PaidAt < end).Select(x => x.Amount).ToListAsync();
    var cafe = cafePayments.Sum();
    var studio = studioPayments.Sum();
    return Results.Ok(new { users = await db.Users.CountAsync(), bookingsToday = await db.Bookings.CountAsync(x => x.BookingDate == today), openTables = await db.Tables.CountAsync(x => x.CurrentSession != null), pendingBookings = await db.Bookings.CountAsync(x => x.Status == "Pending"), paidToday = cafe + studio, cafeRevenue = cafe, studioRevenue = studio });
}).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Manager,Owner" });
secure.MapGet("/admin/users", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await (from u in db.Users join r in db.Roles on u.RoleId equals r.Id orderby u.Id select new StaffDto(u.Id, u.FullName, u.Email, u.Phone, r.Name)).ToListAsync(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Owner" });
secure.MapPut("/admin/users/{id:int}/role", async (int id, RoleRequest request, ClaimsPrincipal actor, IDbContextFactory<CafeDbContext> factory) => {
    if (!InRoles(actor, "Owner")) return Results.Forbid(); var actorId = int.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!); if (id == actorId && request.Role != "Owner") return Results.BadRequest(new { error = "ไม่สามารถลดสิทธิ์บัญชีเจ้าของที่กำลังใช้งานได้" }); await using var db = await factory.CreateDbContextAsync(); var user = await db.Users.FindAsync(id); var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == request.Role);
    if (user is null || role is null || role.Name == "Guest") return Results.BadRequest(new { error = "บัญชีหรือบทบาทไม่ถูกต้อง" });
    user.RoleId = role.Id;
    if (role.Name == "Artist" && !await db.StaffProfiles.AnyAsync(x => x.UserId == id)) db.StaffProfiles.Add(new StaffProfile { UserId = id, Bio = request.Bio, PortfolioUrl = request.PortfolioUrl, Specialties = request.Specialties ?? [] });
    else if (role.Name == "Artist") { var profile = await db.StaffProfiles.SingleAsync(x => x.UserId == id); profile.Bio = request.Bio; profile.PortfolioUrl = request.PortfolioUrl; profile.Specialties = request.Specialties ?? []; }
    await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Owner" });
secure.MapGet("/admin/menu", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await (from item in db.MenuItems join category in db.MenuCategories on item.CategoryId equals category.Id orderby category.Id, item.Id select new MenuDto(item.Id, category.Name, item.Name, item.Price, item.IsAvailable)).ToListAsync(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Manager,Owner" });
secure.MapPost("/admin/menu", async (MenuRequest request, IDbContextFactory<CafeDbContext> factory) => {
    if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 || request.Price < 0 || request.Category.Length > 60) return Results.BadRequest(new { error = "รายละเอียดเมนูไม่ถูกต้อง" });
    await using var db = await factory.CreateDbContextAsync(); var category = await db.MenuCategories.SingleOrDefaultAsync(x => x.Name == request.Category.Trim()); if (category is null) { category = new MenuCategory { Name = request.Category.Trim() }; db.MenuCategories.Add(category); await db.SaveChangesAsync(); }
    var item = new MenuItem { CategoryId = category.Id, Name = request.Name.Trim(), Price = request.Price, IsAvailable = request.IsAvailable }; db.MenuItems.Add(item); await db.SaveChangesAsync(); return Results.Created($"/api/admin/menu/{item.Id}", new MenuDto(item.Id, category.Name, item.Name, item.Price, item.IsAvailable));
}).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Manager,Owner" });
secure.MapPut("/admin/menu/{id:int}", async (int id, MenuRequest request, IDbContextFactory<CafeDbContext> factory) => {
    if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 || request.Price < 0) return Results.BadRequest(new { error = "รายละเอียดเมนูไม่ถูกต้อง" }); await using var db = await factory.CreateDbContextAsync(); var item = await db.MenuItems.FindAsync(id); if (item is null) return Results.NotFound();
    var category = await db.MenuCategories.SingleOrDefaultAsync(x => x.Name == request.Category.Trim()); if (category is null) { category = new MenuCategory { Name = request.Category.Trim() }; db.MenuCategories.Add(category); await db.SaveChangesAsync(); }
    item.Name = request.Name.Trim(); item.Price = request.Price; item.CategoryId = category.Id; item.IsAvailable = request.IsAvailable; await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Manager,Owner" });
secure.MapGet("/admin/services", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return await db.Services.OrderBy(x => x.Id).Select(x => new ServiceDto(x.Id, x.Name, x.Price)).ToListAsync(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Manager,Owner" });
secure.MapPost("/admin/services", async (ServiceRequest request, IDbContextFactory<CafeDbContext> factory) => { if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 || request.Price < 0) return Results.BadRequest(); await using var db = await factory.CreateDbContextAsync(); var item = new Service { Name = request.Name.Trim(), Price = request.Price }; db.Services.Add(item); await db.SaveChangesAsync(); return Results.Created($"/api/admin/services/{item.Id}", new ServiceDto(item.Id, item.Name, item.Price)); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Manager,Owner" });
secure.MapPut("/admin/services/{id:int}", async (int id, ServiceRequest request, IDbContextFactory<CafeDbContext> factory) => { if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100 || request.Price < 0) return Results.BadRequest(); await using var db = await factory.CreateDbContextAsync(); var item = await db.Services.FindAsync(id); if (item is null) return Results.NotFound(); item.Name = request.Name.Trim(); item.Price = request.Price; await db.SaveChangesAsync(); return Results.NoContent(); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "Manager,Owner" });
secure.MapGet("/it/overview", async (IDbContextFactory<CafeDbContext> factory) => { await using var db = await factory.CreateDbContextAsync(); return Results.Ok(new { users = await db.Users.CountAsync(), services = await db.Services.CountAsync(), menuItems = await db.MenuItems.CountAsync(), tables = await db.Tables.CountAsync(), bookings = await db.Bookings.CountAsync(), orders = await db.Orders.CountAsync() }); }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "IT Staff,Manager,Owner" });

app.Run();

static TimeZoneInfo BangkokZone() { try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); } catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } }
static DateTime BangkokToday() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BangkokZone()).Date;
static DateTime BangkokStartUtc(DateTime date) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified), BangkokZone());
static bool InRoles(ClaimsPrincipal user, params string[] roles) => roles.Any(user.IsInRole);
static async Task InitializeDatabase(IServiceProvider services, IConfiguration configuration) {
    await using var scope = services.CreateAsyncScope(); var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CafeDbContext>>(); await using var db = await factory.CreateDbContextAsync(); await db.Database.EnsureCreatedAsync();
    // EnsureCreated does not evolve existing SQLite databases; keep additive upgrades safe for local installs.
    await EnsureColumn(db, "Booking", "CustomerNote"); await EnsureColumn(db, "TableCafe", "OpenedAt"); await EnsureColumn(db, "TableCafe", "BillRequestedAt"); await EnsureColumn(db, "User", "Phone"); await EnsureColumn(db, "OrderItem", "UnitPrice");
    await db.Database.ExecuteSqlRawAsync("UPDATE OrderItem SET UnitPrice = (SELECT Price FROM MenuItem WHERE MenuItem.Id = OrderItem.MenuItemId) WHERE UnitPrice = '0'");
    foreach (var name in new[] { "Guest", "Member", "Artist", "Waiter", "Kitchen", "Cashier", "Manager", "Owner", "IT Staff" }) if (!await db.Roles.AnyAsync(x => x.Name == name)) db.Roles.Add(new Role { Name = name });
    await db.SaveChangesAsync();
    var ownerEmail = configuration["BootstrapOwner:Email"]; var ownerPassword = configuration["BootstrapOwner:Password"];
    if (!string.IsNullOrWhiteSpace(ownerEmail) && !string.IsNullOrWhiteSpace(ownerPassword) && ownerPassword.Length >= 12) {
        var email = ownerEmail.Trim().ToLowerInvariant(); var owner = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (owner is null) { var role = await db.Roles.SingleAsync(x => x.Name == "Owner"); owner = new CafeUser { Email = email, FullName = configuration["BootstrapOwner:FullName"] ?? "ร้าน Cafe & Studio", RoleId = role.Id, CreatedAt = DateTime.UtcNow }; owner.PasswordHash = new PasswordHasher<CafeUser>().HashPassword(owner, ownerPassword); db.Users.Add(owner); }
        else { owner.RoleId = await db.Roles.Where(x => x.Name == "Owner").Select(x => x.Id).SingleAsync(); }
        await db.SaveChangesAsync();
    }
    if (!await db.MenuCategories.AnyAsync()) { var drinks = new MenuCategory { Name = "เครื่องดื่ม" }; var food = new MenuCategory { Name = "อาหาร" }; var bakery = new MenuCategory { Name = "เบเกอรี่" }; db.MenuCategories.AddRange(drinks, food, bakery); await db.SaveChangesAsync(); db.MenuItems.AddRange(new MenuItem { CategoryId = drinks.Id, Name = "อเมริกาโน่", Price = 80 }, new MenuItem { CategoryId = drinks.Id, Name = "คาเฟ่ลาเต้", Price = 95 }, new MenuItem { CategoryId = food.Id, Name = "แซนด์วิช", Price = 120 }, new MenuItem { CategoryId = bakery.Id, Name = "ครัวซองต์เนยสด", Price = 75 }); }
    if (!await db.Services.AnyAsync()) db.Services.AddRange(new Service { Name = "ถ่ายภาพ Portrait", Price = 1800 }, new Service { Name = "แต่งหน้า", Price = 1200 });
    if (!await db.Tables.AnyAsync()) db.Tables.AddRange(Enumerable.Range(1, 8).Select(i => new TableCafe { TableNumber = $"T{i:00}" }));
    if (!await db.StaffProfiles.AnyAsync()) { var roleId = await db.Roles.Where(x => x.Name == "Artist").Select(x => x.Id).SingleAsync(); var artist = new CafeUser { FullName = "ช่างภาพประจำสตูดิโอ", RoleId = roleId, CreatedAt = DateTime.UtcNow }; db.Users.Add(artist); await db.SaveChangesAsync(); db.StaffProfiles.Add(new StaffProfile { UserId = artist.Id, Bio = "ช่างภาพและช่างแต่งหน้าประจำสตูดิโอ", Specialties = ["Portrait", "Makeup"] }); }
    await db.SaveChangesAsync();
}
static async Task EnsureColumn(CafeDbContext db, string table, string column) {
    var upgrade = (table, column) switch {
        ("Booking", "CustomerNote") => ("SELECT name FROM pragma_table_info('Booking')", "ALTER TABLE \"Booking\" ADD COLUMN \"CustomerNote\" TEXT NULL"),
        ("TableCafe", "OpenedAt") => ("SELECT name FROM pragma_table_info('TableCafe')", "ALTER TABLE \"TableCafe\" ADD COLUMN \"OpenedAt\" TEXT NULL"),
        ("TableCafe", "BillRequestedAt") => ("SELECT name FROM pragma_table_info('TableCafe')", "ALTER TABLE \"TableCafe\" ADD COLUMN \"BillRequestedAt\" TEXT NULL"),
        ("User", "Phone") => ("SELECT name FROM pragma_table_info('User')", "ALTER TABLE \"User\" ADD COLUMN \"Phone\" TEXT NULL"),
        ("OrderItem", "UnitPrice") => ("SELECT name FROM pragma_table_info('OrderItem')", "ALTER TABLE \"OrderItem\" ADD COLUMN \"UnitPrice\" TEXT NOT NULL DEFAULT '0'"),
        _ => throw new InvalidOperationException("Unsupported database schema upgrade")
    };
    var connection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
    var wasOpen = connection.State == System.Data.ConnectionState.Open;
    if (!wasOpen) await connection.OpenAsync();
    await using var command = connection.CreateCommand(); command.CommandText = upgrade.Item1;
    var columns = new List<string>(); await using (var reader = await command.ExecuteReaderAsync()) { while (await reader.ReadAsync()) columns.Add(reader.GetString(0)); }
    if (!columns.Contains(column, StringComparer.OrdinalIgnoreCase)) { command.CommandText = upgrade.Item2; await command.ExecuteNonQueryAsync(); }
    if (!wasOpen) await connection.CloseAsync();
}
static async Task<object> CreateToken(CafeDbContext db, CafeUser user) {
    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).TrimEnd('=').Replace('+', '-').Replace('/', '_'); var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    db.LoginSessions.Add(new LoginSession { TokenHash = hash, UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(14) }); await db.SaveChangesAsync();
    return new { accessToken = token, expiresInDays = 14, user = new { user.Id, user.FullName, user.Email, role = user.Role?.Name } };
}
static class BookingSlots { public static readonly string[] All = ["10:00-12:00", "13:00-15:00", "15:00-17:00", "17:00-19:00"]; }
record RegisterRequest(string? Email, string? Password, string? FullName);
record LoginRequest(string Email, string Password);
record ProfileRequest(string? FullName, string? Phone);
record ProfileDto(int Id, string FullName, string? Email, string? Phone, string Role);
record RoleRequest(string Role, string? Bio, string? PortfolioUrl, string[]? Specialties);
record StaffDto(int Id, string FullName, string? Email, string? Phone, string Role);
record MenuRequest(string Name, string Category, decimal Price, bool IsAvailable);
record ServiceRequest(string Name, decimal Price);
record MenuDto(int Id, string Category, string Name, decimal Price, bool IsAvailable);
record ServiceDto(int Id, string Name, decimal Price);
record SlotAvailabilityDto(string TimeSlot, bool Available);
record ArtistDto(int Id, string FullName, string? Bio, string? PortfolioUrl, string[] Specialties);
record BookingDto(int Id, int CustomerId, string CustomerName, int StaffId, string StaffName, int ServiceId, string ServiceName, DateTime BookingDate, string TimeSlot, string Status, decimal TotalPrice, string? Note);
record CreateBookingRequest(int StaffId, int ServiceId, DateTime BookingDate, string TimeSlot, string? Note);
record TableDto(int Id, string TableNumber, bool IsOpen, DateTime? BillRequestedAt);
record TableSessionDto(int Id, string TableNumber, bool BillRequested, List<MenuDto> Menu, List<TableOrderDto> Orders);
record TableOrderDto(int Id, string Status, decimal TotalAmount, List<TableOrderLineDto> Items);
record TableOrderLineDto(int Id, string Menu, int Quantity, decimal Price, string? Note, string KdsStatus);
record OrderRequest(string Session, List<OrderLineRequest>? Items);
record OrderLineRequest(int MenuItemId, int Quantity, string? Note);
record BillRequest(string Session);
record KitchenStatusRequest(string Status);
record KitchenItemDto(int Id, int OrderId, string Table, string Menu, int Quantity, string KdsStatus, string? Note, DateTime CreatedAt);
record PaymentRequest(string Method);
record CashierRow(int TableId, string TableNumber, DateTime? BillRequestedAt, int OrderId, decimal TotalAmount, List<CashierLineDto> Items);
record CashierLineDto(string Menu, int Quantity, decimal Price);
