using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Services;

public sealed class CafeDataService(
    IDbContextFactory<CafeDbContext> factory,
    IPasswordHasher<CafeUser> passwordHasher,
    IConfiguration configuration)
{
    private static readonly string[] Slots = ["10:00-12:00", "13:00-15:00", "15:00-17:00", "17:00-19:00"];
    private static readonly string[] Roles = ["Member", "Artist", "Waiter", "Kitchen", "Cashier", "Manager", "Owner", "IT Staff"];

    public async Task InitializeAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        // WAL lets concurrent screens read while a write is in progress. SQLite
        // keeps this journal mode in the database file after initialization.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await EnsureColumnAsync(db, "Booking", "CustomerNote", "ALTER TABLE \"Booking\" ADD COLUMN \"CustomerNote\" TEXT NULL");
        await EnsureColumnAsync(db, "TableCafe", "OpenedAt", "ALTER TABLE \"TableCafe\" ADD COLUMN \"OpenedAt\" TEXT NULL");
        await EnsureColumnAsync(db, "TableCafe", "BillRequestedAt", "ALTER TABLE \"TableCafe\" ADD COLUMN \"BillRequestedAt\" TEXT NULL");
        await EnsureColumnAsync(db, "User", "Phone", "ALTER TABLE \"User\" ADD COLUMN \"Phone\" TEXT NULL");
        await EnsureColumnAsync(db, "OrderItem", "UnitPrice", "ALTER TABLE \"OrderItem\" ADD COLUMN \"UnitPrice\" TEXT NOT NULL DEFAULT '0'");
        await db.Database.ExecuteSqlRawAsync("UPDATE OrderItem SET UnitPrice = (SELECT Price FROM MenuItem WHERE MenuItem.Id = OrderItem.MenuItemId) WHERE UnitPrice = '0'");

        foreach (var name in new[] { "Guest", "Member", "Artist", "Waiter", "Kitchen", "Cashier", "Manager", "Owner", "IT Staff" })
            if (!await db.Roles.AnyAsync(x => x.Name == name)) db.Roles.Add(new Role { Name = name });
        await db.SaveChangesAsync();

        await EnsureLocalStaffAccountsAsync(db);

        var ownerEmail = configuration["BootstrapOwner:Email"]?.Trim().ToLowerInvariant();
        var ownerPassword = configuration["BootstrapOwner:Password"];
        if (!string.IsNullOrWhiteSpace(ownerEmail))
        {
            var ownerRole = await db.Roles.SingleAsync(x => x.Name == "Owner");
            var owner = await db.Users.SingleOrDefaultAsync(x => x.Email == ownerEmail);
            if (owner is not null)
            {
                owner.RoleId = ownerRole.Id;
                await db.SaveChangesAsync();
            }
            else if (!string.IsNullOrWhiteSpace(ownerPassword) && ownerPassword.Length >= 12)
            {
                owner = new CafeUser { Email = ownerEmail, FullName = configuration["BootstrapOwner:FullName"] ?? "ร้าน Cafe & Studio", RoleId = ownerRole.Id, CreatedAt = DateTime.UtcNow };
                owner.PasswordHash = passwordHasher.HashPassword(owner, ownerPassword);
                db.Users.Add(owner);
                await db.SaveChangesAsync();
            }
        }

        if (!await db.MenuCategories.AnyAsync())
        {
            var drinks = new MenuCategory { Name = "เครื่องดื่ม" };
            var food = new MenuCategory { Name = "อาหาร" };
            var bakery = new MenuCategory { Name = "เบเกอรี่" };
            db.MenuCategories.AddRange(drinks, food, bakery);
            await db.SaveChangesAsync();
            db.MenuItems.AddRange(
                new MenuItem { CategoryId = drinks.Id, Name = "อเมริกาโน่", Price = 80 },
                new MenuItem { CategoryId = drinks.Id, Name = "คาเฟ่ลาเต้", Price = 95 },
                new MenuItem { CategoryId = food.Id, Name = "แซนด์วิช", Price = 120 },
                new MenuItem { CategoryId = bakery.Id, Name = "ครัวซองต์เนยสด", Price = 75 });
        }
        if (!await db.Services.AnyAsync()) db.Services.AddRange(new Service { Name = "ถ่ายภาพ Portrait", Price = 1800 }, new Service { Name = "แต่งหน้า", Price = 1200 });
        if (!await db.Tables.AnyAsync()) db.Tables.AddRange(Enumerable.Range(1, 8).Select(i => new TableCafe { TableNumber = $"T{i:00}" }));
        if (!await db.StaffProfiles.AnyAsync())
        {
            var artistRoleId = await db.Roles.Where(x => x.Name == "Artist").Select(x => x.Id).SingleAsync();
            var artist = await db.Users.FirstOrDefaultAsync(x => x.RoleId == artistRoleId);
            if (artist is null)
            {
                artist = new CafeUser { FullName = "ช่างภาพประจำสตูดิโอ", RoleId = artistRoleId, CreatedAt = DateTime.UtcNow };
                db.Users.Add(artist);
                await db.SaveChangesAsync();
            }
            db.StaffProfiles.Add(new StaffProfile { UserId = artist.Id, Bio = "ช่างภาพและช่างแต่งหน้าประจำสตูดิโอ", Specialties = ["Portrait", "Makeup"] });
        }
        await db.SaveChangesAsync();
    }

    private async Task EnsureLocalStaffAccountsAsync(CafeDbContext db)
    {
        var directory = Path.GetDirectoryName(db.Database.GetDbConnection().DataSource)!;
        Directory.CreateDirectory(directory);
        var credentialsPath = Path.Combine(directory, "local-accounts.json");
        List<LocalAccountCredential> credentials;
        if (File.Exists(credentialsPath))
        {
            credentials = JsonSerializer.Deserialize<List<LocalAccountCredential>>(await File.ReadAllTextAsync(credentialsPath)) ?? [];
        }
        else
        {
            credentials =
            [
                NewCredential("artist@cafe.local", "ช่างภาพ/ช่างแต่งหน้า", "Artist"),
                NewCredential("waiter@cafe.local", "พนักงานบริการ", "Waiter"),
                NewCredential("kitchen@cafe.local", "พนักงานครัว", "Kitchen"),
                NewCredential("cashier@cafe.local", "แคชเชียร์", "Cashier"),
                NewCredential("manager@cafe.local", "ผู้จัดการร้าน", "Manager"),
                NewCredential("it@cafe.local", "ผู้ดูแลระบบ IT", "IT Staff"),
                NewCredential("owner@cafe.local", "เจ้าของร้าน", "Owner")
            ];
            await File.WriteAllTextAsync(credentialsPath, JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true }));
        }

        foreach (var credential in credentials)
        {
            var email = credential.Email.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
            if (user is null)
            {
                var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == credential.Role);
                if (role is null) continue;
                user = new CafeUser { Email = email, FullName = credential.FullName, RoleId = role.Id, CreatedAt = DateTime.UtcNow };
                user.PasswordHash = passwordHasher.HashPassword(user, credential.Password);
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            if (credential.Role == "Artist" && !await db.StaffProfiles.AnyAsync(x => x.UserId == user.Id))
                db.StaffProfiles.Add(new StaffProfile { UserId = user.Id, Bio = "ช่างภาพและช่างแต่งหน้าประจำสตูดิโอ", Specialties = ["Portrait", "Makeup"] });
        }
        await db.SaveChangesAsync();
    }

    private static LocalAccountCredential NewCredential(string email, string fullName, string role) =>
        new(email, fullName, role, Convert.ToBase64String(RandomNumberGenerator.GetBytes(18)).TrimEnd('=').Replace('+', '-').Replace('/', '_'));

    private sealed record LocalAccountCredential(string Email, string FullName, string Role, string Password);

    public async Task<HomeViewModel> GetHomeAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return new HomeViewModel { Menu = await MenuQuery(db, availableOnly: true), Artists = await ArtistQuery(db), Services = await db.Services.OrderBy(x => x.Id).Select(x => new ServiceDto(x.Id, x.Name, x.Price)).ToListAsync() };
    }

    public async Task<(CafeUser? User, string? Error)> RegisterAsync(RegisterForm form)
    {
        var email = form.Email.Trim().ToLowerInvariant();
        if (form.Password.Length < 8) return (null, "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร");
        await using var db = await factory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(x => x.Email == email)) return (null, "อีเมลนี้มีบัญชีแล้ว");
        var memberRole = await db.Roles.SingleAsync(x => x.Name == "Member");
        var user = new CafeUser { Email = email, FullName = form.FullName.Trim(), RoleId = memberRole.Id, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = passwordHasher.HashPassword(user, form.Password);
        db.Users.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return (null, "อีเมลนี้มีบัญชีแล้ว"); }
        user.Role = memberRole;
        return (user, null);
    }

    public async Task<(CafeUser? User, string? Error)> LoginAsync(LoginForm form)
    {
        await using var db = await factory.CreateDbContextAsync();
        var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Email == form.Email.Trim().ToLowerInvariant());
        if (user?.PasswordHash is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, form.Password) == PasswordVerificationResult.Failed)
            return (null, "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
        return (user, null);
    }

    public async Task<ProfileDto?> GetProfileAsync(int userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Users.Where(x => x.Id == userId).Select(x => new ProfileDto(x.Id, x.FullName, x.Email, x.Phone, x.Role.Name)).SingleOrDefaultAsync();
    }

    public async Task<bool> UpdateProfileAsync(int userId, ProfileForm form)
    {
        await using var db = await factory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return false;
        user.FullName = form.FullName.Trim();
        user.Phone = form.Phone?.Trim();
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<BookingPageViewModel> GetBookingPageAsync(DateTime date, int staffId, BookingForm? form = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var slots = Slots.Select(x => new SlotDto(x, true)).ToList();
        if (staffId > 0)
        {
            var taken = await db.Bookings.Where(x => x.StaffId == staffId && x.BookingDate == date.Date && x.Status != "Cancelled").Select(x => x.TimeSlot).ToListAsync();
            slots = Slots.Select(x => new SlotDto(x, !taken.Contains(x))).ToList();
        }
        return new BookingPageViewModel
        {
            Artists = await ArtistQuery(db),
            Services = await db.Services.OrderBy(x => x.Id).Select(x => new ServiceDto(x.Id, x.Name, x.Price)).ToListAsync(),
            Slots = slots,
            Form = form ?? new BookingForm { BookingDate = date, StaffId = staffId }
        };
    }

    public async Task<List<BookingDto>> GetBookingsAsync(int userId, string role)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Bookings.AsQueryable();
        if (role == "Member") query = query.Where(x => x.CustomerId == userId);
        else if (role == "Artist") query = query.Where(x => x.StaffId == userId);
        else if (role is not ("Waiter" or "Cashier" or "Manager" or "Owner")) return [];
        return await BookingQuery(db, query);
    }

    public async Task<(bool Ok, string? Error)> CreateBookingAsync(int customerId, string role, BookingForm form)
    {
        if (role is not ("Member" or "Artist" or "Waiter" or "Manager" or "Owner")) return (false, "บัญชีนี้ไม่มีสิทธิ์จองคิว");
        if (!Slots.Contains(form.TimeSlot) || form.BookingDate.Date < BangkokToday() || form.Note?.Length > 500) return (false, "วันที่หรือรอบเวลาไม่ถูกต้อง");
        await using var db = await factory.CreateDbContextAsync();
        var artist = await (from user in db.Users join roleRow in db.Roles on user.RoleId equals roleRow.Id join profile in db.StaffProfiles on user.Id equals profile.UserId where user.Id == form.StaffId && roleRow.Name == "Artist" select user.Id).AnyAsync();
        var service = await db.Services.FindAsync(form.ServiceId);
        if (!artist || service is null) return (false, "ไม่พบบริการหรือช่าง");
        var booking = new Booking { CustomerId = customerId, StaffId = form.StaffId, ServiceId = service.Id, BookingDate = form.BookingDate.Date, TimeSlot = form.TimeSlot, TotalPrice = service.Price, CustomerNote = form.Note?.Trim(), Status = "Pending", CreatedAt = DateTime.UtcNow };
        db.Bookings.Add(booking);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return (false, "รอบเวลานี้ถูกจองแล้ว"); }
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CancelBookingAsync(int id, int userId, string role)
    {
        if (role is not ("Member" or "Artist" or "Waiter" or "Manager" or "Owner")) return (false, "บัญชีนี้ไม่มีสิทธิ์ยกเลิกรายการจอง");
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Bookings.Where(x => x.Id == id && x.Status == "Pending");
        if (role == "Member") query = query.Where(x => x.CustomerId == userId);
        else if (role == "Artist") query = query.Where(x => x.StaffId == userId);
        var booking = await query.SingleOrDefaultAsync();
        if (booking is null) return (false, "ไม่พบรายการจองที่ยกเลิกได้");
        booking.Status = "Cancelled";
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<TableSessionDto?> GetTableSessionAsync(int id, string session)
    {
        await using var db = await factory.CreateDbContextAsync();
        var table = await db.Tables.SingleOrDefaultAsync(x => x.Id == id && x.CurrentSession == session);
        if (table is null) return null;
        var orders = await db.Orders.Where(x => x.TableId == id && (x.Status == "Dining" || x.Status == "Billing")).OrderBy(x => x.Id).ToListAsync();
        var orderIds = orders.Select(x => x.Id).ToArray();
        var lines = await (from line in db.OrderItems join menu in db.MenuItems on line.MenuItemId equals menu.Id where orderIds.Contains(line.OrderId) select new { line.OrderId, line.Id, menu.Name, line.Quantity, Price = line.UnitPrice, line.Note, line.KdsStatus }).ToListAsync();
        var menuItems = await MenuQuery(db, availableOnly: true);
        var orderDtos = orders.Select(order => new TableOrderDto(order.Id, order.Status, order.TotalAmount, lines.Where(line => line.OrderId == order.Id).Select(line => new TableOrderLineDto(line.Id, line.Name, line.Quantity, line.Price, line.Note, line.KdsStatus)).ToList())).ToList();
        return new TableSessionDto(table.Id, table.TableNumber, table.BillRequestedAt is not null, menuItems, orderDtos);
    }

    public async Task<(OpenedTableDto? Table, string? Error)> OpenTableAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var table = await db.Tables.FindAsync(id);
        if (table is null || table.CurrentSession is not null) return (null, "โต๊ะไม่ว่างหรือไม่พบโต๊ะ");
        table.CurrentSession = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        table.OpenedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return (new OpenedTableDto(table.Id, table.TableNumber, table.CurrentSession, $"/table/{table.Id}?session={table.CurrentSession}"), null);
    }

    public async Task<(bool Ok, string? Error)> PlaceOrderAsync(int id, TableOrderForm form)
    {
        var lines = form.Items.Where(x => x.Quantity > 0).ToList();
        if (lines.Count is < 1 or > 30 || lines.Any(x => x.Quantity > 20 || x.Note?.Length > 180)) return (false, "รายการสั่งซื้อไม่ถูกต้อง");
        await using var db = await factory.CreateDbContextAsync();
        var table = await db.Tables.SingleOrDefaultAsync(x => x.Id == id && x.CurrentSession == form.Session);
        if (table is null) return (false, "QR session หมดอายุ กรุณาเรียกพนักงาน");
        var ids = lines.Select(x => x.MenuItemId).Distinct().ToArray();
        var menu = await db.MenuItems.Where(x => ids.Contains(x.Id) && x.IsAvailable).ToDictionaryAsync(x => x.Id);
        if (menu.Count != ids.Length) return (false, "มีเมนูที่ไม่พร้อมจำหน่าย");
        var order = await db.Orders.Where(x => x.TableId == id && x.Status == "Dining").OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        if (order is null) { order = new CafeOrder { TableId = id, Status = "Dining", CreatedAt = DateTime.UtcNow }; db.Orders.Add(order); await db.SaveChangesAsync(); }
        foreach (var line in lines) db.OrderItems.Add(new OrderItem { OrderId = order.Id, MenuItemId = line.MenuItemId, Quantity = line.Quantity, UnitPrice = menu[line.MenuItemId].Price, Note = line.Note?.Trim(), KdsStatus = "Pending" });
        table.BillRequestedAt = null;
        await db.SaveChangesAsync();
        var totals = await db.OrderItems.Where(x => x.OrderId == order.Id).Select(x => new { x.UnitPrice, x.Quantity }).ToListAsync();
        order.TotalAmount = totals.Sum(x => x.UnitPrice * x.Quantity);
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RequestBillAsync(int id, string session)
    {
        await using var db = await factory.CreateDbContextAsync();
        var table = await db.Tables.SingleOrDefaultAsync(x => x.Id == id && x.CurrentSession == session);
        if (table is null) return (false, "QR session หมดอายุ กรุณาเรียกพนักงาน");
        var orders = await db.Orders.Where(x => x.TableId == id && x.Status == "Dining").ToListAsync();
        if (orders.Count == 0) return (false, "โต๊ะนี้ยังไม่มีรายการ");
        foreach (var order in orders) order.Status = "Billing";
        table.BillRequestedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<AdminViewModel> GetAdminAsync(string role)
    {
        await using var db = await factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var start = BangkokStartUtc(BangkokToday());
        var end = start.AddDays(1);
        var cafePayments = await db.Payments.Where(x => x.Status == "Success" && x.ReferenceType == "ORDER" && x.PaidAt >= start && x.PaidAt < end).Select(x => x.Amount).ToListAsync();
        var studioPayments = await db.Payments.Where(x => x.Status == "Success" && x.ReferenceType == "BOOKING" && x.PaidAt >= start && x.PaidAt < end).Select(x => x.Amount).ToListAsync();
        var overview = new OverviewDto(await db.Users.CountAsync(), await db.Bookings.CountAsync(x => x.BookingDate == BangkokToday()), await db.Tables.CountAsync(x => x.CurrentSession != null), await db.Bookings.CountAsync(x => x.Status == "Pending"), cafePayments.Sum() + studioPayments.Sum(), cafePayments.Sum(), studioPayments.Sum());
        var menu = await MenuQuery(db, availableOnly: false);
        var services = await db.Services.OrderBy(x => x.Id).Select(x => new ServiceDto(x.Id, x.Name, x.Price)).ToListAsync();
        var bookings = await BookingQuery(db, db.Bookings.AsQueryable());
        var staff = role == "Owner" ? await (from user in db.Users join userRole in db.Roles on user.RoleId equals userRole.Id orderby user.Id select new StaffDto(user.Id, user.FullName, user.Email, user.Phone, userRole.Name)).ToListAsync() : [];
        return new AdminViewModel { Overview = overview, Bookings = bookings, Menu = menu, Services = services, Staff = staff };
    }

    public async Task<(bool Ok, string? Error)> SaveMenuAsync(MenuForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Name) || form.Name.Length > 100 || form.Price < 0 || string.IsNullOrWhiteSpace(form.Category) || form.Category.Length > 60) return (false, "รายละเอียดเมนูไม่ถูกต้อง");
        await using var db = await factory.CreateDbContextAsync();
        var categoryName = form.Category.Trim();
        var category = await db.MenuCategories.SingleOrDefaultAsync(x => x.Name == categoryName);
        if (category is null) { category = new MenuCategory { Name = categoryName }; db.MenuCategories.Add(category); await db.SaveChangesAsync(); }
        if (form.Id == 0) db.MenuItems.Add(new MenuItem { CategoryId = category.Id, Name = form.Name.Trim(), Price = form.Price, IsAvailable = form.IsAvailable });
        else
        {
            var item = await db.MenuItems.FindAsync(form.Id);
            if (item is null) return (false, "ไม่พบเมนู");
            item.CategoryId = category.Id; item.Name = form.Name.Trim(); item.Price = form.Price; item.IsAvailable = form.IsAvailable;
        }
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SaveServiceAsync(ServiceForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Name) || form.Name.Length > 100 || form.Price < 0) return (false, "รายละเอียดบริการไม่ถูกต้อง");
        await using var db = await factory.CreateDbContextAsync();
        if (form.Id == 0) db.Services.Add(new Service { Name = form.Name.Trim(), Price = form.Price });
        else
        {
            var service = await db.Services.FindAsync(form.Id);
            if (service is null) return (false, "ไม่พบบริการ");
            service.Name = form.Name.Trim(); service.Price = form.Price;
        }
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateRoleAsync(int actorId, RoleForm form)
    {
        if (actorId == form.Id && form.Role != "Owner") return (false, "ไม่สามารถลดสิทธิ์บัญชีเจ้าของที่กำลังใช้งานได้");
        if (!Roles.Contains(form.Role)) return (false, "บทบาทไม่ถูกต้อง");
        await using var db = await factory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(form.Id);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == form.Role);
        if (user is null || role is null) return (false, "ไม่พบบัญชีหรือบทบาท");
        user.RoleId = role.Id;
        if (role.Name == "Artist")
        {
            var profile = await db.StaffProfiles.SingleOrDefaultAsync(x => x.UserId == user.Id);
            if (profile is null) db.StaffProfiles.Add(new StaffProfile { UserId = user.Id, Bio = form.Bio, PortfolioUrl = form.PortfolioUrl, Specialties = [] });
            else { profile.Bio = form.Bio; profile.PortfolioUrl = form.PortfolioUrl; }
        }
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<TableDto>> GetTablesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Tables.OrderBy(x => x.Id).Select(x => new TableDto(x.Id, x.TableNumber, x.CurrentSession != null, x.BillRequestedAt, x.CurrentSession)).ToListAsync();
    }

    public async Task<KdsViewModel> GetKdsAsync(bool summary)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (summary)
        {
            var groups = await (from line in db.OrderItems join order in db.Orders on line.OrderId equals order.Id join menu in db.MenuItems on line.MenuItemId equals menu.Id where order.Status != "Paid" && line.KdsStatus != "Served" group line by menu.Name into g select new { Menu = g.Key, Quantity = g.Sum(x => x.Quantity) }).OrderBy(x => x.Menu).ToListAsync();
            return new KdsViewModel { Summary = true, SummaryItems = groups.Select(x => new KitchenSummaryDto(x.Menu, x.Quantity)).ToList() };
        }
        var rows = await (from line in db.OrderItems join order in db.Orders on line.OrderId equals order.Id join table in db.Tables on order.TableId equals table.Id join menu in db.MenuItems on line.MenuItemId equals menu.Id where order.Status != "Paid" orderby order.CreatedAt select new KitchenItemDto(line.Id, order.Id, table.TableNumber, menu.Name, line.Quantity, line.KdsStatus, line.Note, order.CreatedAt)).ToListAsync();
        return new KdsViewModel { Items = rows };
    }

    public async Task<(bool Ok, string? Error)> SetKitchenStatusAsync(int id, string status)
    {
        await using var db = await factory.CreateDbContextAsync();
        var line = await db.OrderItems.FindAsync(id);
        if (line is null) return (false, "ไม่พบรายการอาหาร");
        var next = line.KdsStatus == "Pending" ? "Cooking" : line.KdsStatus == "Cooking" ? "Served" : null;
        if (next != status) return (false, "เปลี่ยนสถานะได้ครั้งละหนึ่งขั้น");
        line.KdsStatus = next;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<CashierViewModel> GetCashierAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var orders = await db.Orders.Where(x => x.Status == "Billing").OrderBy(x => x.Id).ToListAsync();
        var rows = new List<CashierRow>();
        foreach (var order in orders)
        {
            var table = await db.Tables.FindAsync(order.TableId);
            if (table is null) continue;
            var items = await (from line in db.OrderItems join menu in db.MenuItems on line.MenuItemId equals menu.Id where line.OrderId == order.Id select new CashierLineDto(menu.Name, line.Quantity, line.UnitPrice)).ToListAsync();
            rows.Add(new CashierRow(table.Id, table.TableNumber, table.BillRequestedAt, order.Id, order.TotalAmount, items));
        }
        var paidTables = new List<CashierPaidTableDto>();
        var openTables = await db.Tables.Where(x => x.CurrentSession != null && x.OpenedAt != null).OrderBy(x => x.Id).ToListAsync();
        foreach (var table in openTables)
        {
            var sessionOrders = await db.Orders.Where(x => x.TableId == table.Id && x.CreatedAt >= table.OpenedAt).ToListAsync();
            if (sessionOrders.Count > 0 && sessionOrders.All(x => x.Status == "Paid"))
                paidTables.Add(new CashierPaidTableDto(table.Id, table.TableNumber, sessionOrders.Sum(x => x.TotalAmount)));
        }
        var bookings = await BookingQuery(db, db.Bookings.Where(x => x.Status == "Pending"));
        return new CashierViewModel { Bills = rows, PaidTables = paidTables, PendingBookings = bookings };
    }

    public async Task<(bool Ok, string? Error)> PayTableAsync(int id, string method)
    {
        if (method is not ("CASH" or "PROMPTPAY")) return (false, "ช่องทางชำระเงินไม่ถูกต้อง");
        await using var db = await factory.CreateDbContextAsync();
        var table = await db.Tables.FindAsync(id);
        var orders = await db.Orders.Where(x => x.TableId == id && x.Status == "Billing").ToListAsync();
        if (table?.CurrentSession is null || orders.Count == 0) return (false, "ไม่พบบิลที่รอชำระ");
        var orderIds = orders.Select(x => x.Id).ToArray();
        if (await db.OrderItems.AnyAsync(x => orderIds.Contains(x.OrderId) && x.KdsStatus != "Served")) return (false, "ยังมีรายการที่ครัวทำไม่เสร็จ");
        foreach (var order in orders) { order.Status = "Paid"; db.Payments.Add(new Payment { ReferenceId = order.Id, ReferenceType = "ORDER", Amount = order.TotalAmount, Method = method, Status = "Success", PaidAt = DateTime.UtcNow }); }
        table.CurrentSession = null;
        table.OpenedAt = null;
        table.BillRequestedAt = null;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ClosePaidTableAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var table = await db.Tables.FindAsync(id);
        if (table?.CurrentSession is null || table.OpenedAt is null) return (false, "โต๊ะนี้ปิดไปแล้วหรือไม่มีรอบใช้งาน");
        var orders = await db.Orders.Where(x => x.TableId == id && x.CreatedAt >= table.OpenedAt).ToListAsync();
        if (orders.Count == 0 || orders.Any(x => x.Status != "Paid")) return (false, "กรุณารับชำระทุกรายการของโต๊ะนี้ก่อนปิดโต๊ะ");
        table.CurrentSession = null;
        table.OpenedAt = null;
        table.BillRequestedAt = null;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> MarkBookingPaidAsync(int id, string method)
    {
        if (method is not ("CASH" or "PROMPTPAY")) return (false, "ช่องทางชำระเงินไม่ถูกต้อง");
        await using var db = await factory.CreateDbContextAsync();
        var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Pending");
        if (booking is null) return (false, "ไม่พบรายการค้างชำระ");
        booking.Status = "Paid";
        db.Payments.Add(new Payment { ReferenceId = id, ReferenceType = "BOOKING", Amount = booking.TotalPrice, Method = method, Status = "Success", PaidAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CompleteBookingAsync(int id, int actorId, string role)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Bookings.Where(x => x.Id == id && x.Status == "Paid");
        if (role == "Artist") query = query.Where(x => x.StaffId == actorId);
        var booking = await query.SingleOrDefaultAsync();
        if (booking is null) return (false, "ต้องเป็นรายการที่ชำระแล้วและอยู่ในตารางของคุณ");
        booking.Status = "Completed";
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<string> GetItOverviewAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var summary = new { users = await db.Users.CountAsync(), services = await db.Services.CountAsync(), menuItems = await db.MenuItems.CountAsync(), tables = await db.Tables.CountAsync(), bookings = await db.Bookings.CountAsync(), orders = await db.Orders.CountAsync() };
        return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<string?> GetRoleAsync(int? userId)
    {
        if (userId is null) return null;
        await using var db = await factory.CreateDbContextAsync();
        return await db.Users.Where(x => x.Id == userId).Select(x => x.Role.Name).SingleOrDefaultAsync();
    }

    private static Task<List<MenuItemDto>> MenuQuery(CafeDbContext db, bool availableOnly)
    {
        var query = from item in db.MenuItems join category in db.MenuCategories on item.CategoryId equals category.Id select new { item, category };
        if (availableOnly) query = query.Where(x => x.item.IsAvailable);
        return query.OrderBy(x => x.category.Id).ThenBy(x => x.item.Id).Select(x => new MenuItemDto(x.item.Id, x.category.Name, x.item.Name, x.item.Price, x.item.IsAvailable)).ToListAsync();
    }

    private static Task<List<ArtistDto>> ArtistQuery(CafeDbContext db) =>
        (from user in db.Users join role in db.Roles on user.RoleId equals role.Id join profile in db.StaffProfiles on user.Id equals profile.UserId where role.Name == "Artist" orderby user.Id select new ArtistDto(user.Id, user.FullName, profile.Bio, profile.PortfolioUrl, profile.Specialties)).ToListAsync();

    private static Task<List<BookingDto>> BookingQuery(CafeDbContext db, IQueryable<Booking> bookings) =>
        (from booking in bookings join customer in db.Users on booking.CustomerId equals customer.Id join artist in db.Users on booking.StaffId equals artist.Id join service in db.Services on booking.ServiceId equals service.Id orderby booking.BookingDate, booking.TimeSlot select new BookingDto(booking.Id, booking.CustomerId, customer.FullName, booking.StaffId, artist.FullName, booking.ServiceId, service.Name, booking.BookingDate, booking.TimeSlot, booking.Status, booking.TotalPrice, booking.CustomerNote)).ToListAsync();

    private static TimeZoneInfo BangkokZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
    }

    private static DateTime BangkokToday() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BangkokZone()).Date;
    private static DateTime BangkokStartUtc(DateTime date) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified), BangkokZone());

    private static async Task EnsureColumnAsync(CafeDbContext db, string table, string column, string alterSql)
    {
        var connection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name FROM pragma_table_info('{table}')";
        var columns = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) columns.Add(reader.GetString(0));
        if (!columns.Contains(column, StringComparer.OrdinalIgnoreCase)) { command.CommandText = alterSql; await command.ExecuteNonQueryAsync(); }
        if (!wasOpen) await connection.CloseAsync();
    }
}
