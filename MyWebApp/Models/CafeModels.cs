using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public sealed record MenuItemDto(int Id, string Category, string Name, decimal Price, bool IsAvailable = true);
public sealed record ServiceDto(int Id, string Name, decimal Price);
public sealed record ArtistDto(int Id, string FullName, string? Bio, string? PortfolioUrl, string[] Specialties);
public sealed record SlotDto(string TimeSlot, bool Available);
public sealed record ProfileDto(int Id, string FullName, string? Email, string? Phone, string Role);
public sealed record BookingDto(int Id, int CustomerId, string CustomerName, int StaffId, string StaffName, int ServiceId, string ServiceName, DateTime BookingDate, string TimeSlot, string Status, decimal TotalPrice, string? Note);
public sealed record KitchenItemDto(int Id, int OrderId, string Table, string Menu, int Quantity, string KdsStatus, string? Note, DateTime CreatedAt);
public sealed record CashierLineDto(string Menu, int Quantity, decimal Price);
public sealed record CashierRow(int TableId, string TableNumber, DateTime? BillRequestedAt, int OrderId, decimal TotalAmount, List<CashierLineDto> Items);
public sealed record OverviewDto(int Users, int BookingsToday, int OpenTables, int PendingBookings, decimal PaidToday, decimal CafeRevenue, decimal StudioRevenue);
public sealed record StaffDto(int Id, string FullName, string? Email, string? Phone, string Role);
public sealed record TableDto(int Id, string TableNumber, bool IsOpen, DateTime? BillRequestedAt);
public sealed record OpenedTableDto(int Id, string TableNumber, string Session, string Url);
public sealed record TableSessionDto(int Id, string TableNumber, bool BillRequested, List<MenuItemDto> Menu, List<TableOrderDto> Orders);
public sealed record TableOrderDto(int Id, string Status, decimal TotalAmount, List<TableOrderLineDto> Items);
public sealed record TableOrderLineDto(int Id, string Menu, int Quantity, decimal Price, string? Note, string KdsStatus);

public sealed class RegisterForm { [Required, StringLength(100, MinimumLength=2)] public string FullName { get; set; } = ""; [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = ""; [Required, MinLength(8), StringLength(100)] public string Password { get; set; } = ""; }
public sealed class LoginForm { [Required, EmailAddress] public string Email { get; set; } = ""; [Required] public string Password { get; set; } = ""; public string? ReturnUrl { get; set; } }
public sealed class ProfileForm { [Required, StringLength(100,MinimumLength=2)] public string FullName { get; set; } = ""; [Phone, StringLength(30)] public string? Phone { get; set; } public string? Email { get; set; } public string? Role { get; set; } }
public sealed class BookingForm { [Range(1,int.MaxValue)] public int StaffId { get; set; } [Range(1,int.MaxValue)] public int ServiceId { get; set; } [DataType(DataType.Date)] public DateTime BookingDate { get; set; } = DateTime.Today; [Required] public string TimeSlot { get; set; } = ""; [StringLength(500)] public string? Note { get; set; } }
public sealed class MenuForm { public int Id { get; set; } [Required, StringLength(100)] public string Name { get; set; } = ""; [Required, StringLength(60)] public string Category { get; set; } = ""; [Range(0,1000000)] public decimal Price { get; set; } public bool IsAvailable { get; set; } = true; }
public sealed class ServiceForm { public int Id { get; set; } [Required,StringLength(100)] public string Name { get; set; } = ""; [Range(0,1000000)] public decimal Price { get; set; } }
public sealed class RoleForm { public int Id { get; set; } [Required] public string Role { get; set; } = ""; [StringLength(1000)] public string? Bio { get; set; } [Url, StringLength(500)] public string? PortfolioUrl { get; set; } }
public sealed class AdminViewModel { public OverviewDto? Overview { get; init; } public List<BookingDto> Bookings { get; init; }=[]; public List<MenuItemDto> Menu { get; init; }=[]; public List<ServiceDto> Services { get; init; }=[]; public List<StaffDto> Staff { get; init; }=[]; }
public sealed record KitchenSummaryDto(string Menu, int Quantity);
public sealed class KdsViewModel { public List<KitchenItemDto> Items { get; init; }=[]; public List<KitchenSummaryDto> SummaryItems {get;init;}=[]; public bool Summary { get; init; } }
public sealed class CashierViewModel { public List<CashierRow> Bills {get;init;}=[];public List<BookingDto> PendingBookings {get;init;}=[]; }
public sealed class HomeViewModel { public List<MenuItemDto> Menu { get; init; }=[]; public List<ArtistDto> Artists { get; init; }=[]; public List<ServiceDto> Services { get; init; }=[]; public string? Error { get; init; } }
public sealed class BookingPageViewModel { public List<ArtistDto> Artists { get; init; }=[]; public List<ServiceDto> Services { get; init; }=[]; public List<SlotDto> Slots { get; init; }=[]; public BookingForm Form { get; init; }=new(); public string? Error { get; init; } }
public sealed class TableOrderForm { public string Session { get; set; }=""; public List<TableOrderInput> Items { get; set; }=[]; }
public sealed class TableOrderInput { public int MenuItemId { get; set; } public int Quantity { get; set; } public string? Note { get; set; } }
public sealed class TablePageViewModel { public TableSessionDto SessionInfo { get; init; }=new(0,"",false,[],[]); public string Token { get; init; }=""; public string? Error { get; init; } }
