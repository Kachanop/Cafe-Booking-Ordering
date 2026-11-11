using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CafeSystem.Frontend.Services;

public sealed class FrontendSession { public string? Token { get; set; } public string? Name { get; set; } public string? Role { get; set; } }
public sealed class CafeApi(HttpClient http, FrontendSession session)
{
    private void AddAuth() { http.DefaultRequestHeaders.Authorization = session.Token is null ? null : new AuthenticationHeaderValue("Bearer", session.Token); }
    public async Task<List<MenuItemDto>> GetMenu() => await http.GetFromJsonAsync<List<MenuItemDto>>("api/menu") ?? [];
    public async Task<List<ServiceDto>> GetServices() => await http.GetFromJsonAsync<List<ServiceDto>>("api/services") ?? [];
    public async Task<List<ArtistDto>> GetArtists() => await http.GetFromJsonAsync<List<ArtistDto>>("api/artists") ?? [];
    public async Task<AuthResult?> Login(string email, string password) { var result = await http.PostAsJsonAsync("api/auth/login", new { email, password }); if (!result.IsSuccessStatusCode) return null; var auth = await result.Content.ReadFromJsonAsync<AuthResult>(); if (auth is not null) { session.Token = auth.AccessToken; session.Name = auth.User.FullName; session.Role = auth.User.Role; } return auth; }
    public async Task<(bool ok,string? error)> Register(string email, string password, string fullName) { var result = await http.PostAsJsonAsync("api/auth/register", new { email, password, fullName }); if (!result.IsSuccessStatusCode) return (false,(await result.Content.ReadFromJsonAsync<ApiError>())?.Error ?? "สมัครสมาชิกไม่สำเร็จ"); var auth=await result.Content.ReadFromJsonAsync<AuthResult>(); if(auth is not null){session.Token=auth.AccessToken;session.Name=auth.User.FullName;session.Role=auth.User.Role;} return (true,null); }
    public async Task<List<BookingDto>> GetBookings() { AddAuth(); var result=await http.GetAsync("api/bookings"); result.EnsureSuccessStatusCode(); return await result.Content.ReadFromJsonAsync<List<BookingDto>>() ?? []; }
    public async Task<(bool ok,string? error)> CreateBooking(int staffId,int serviceId,DateTime date,string slot,string? note) { AddAuth(); var result=await http.PostAsJsonAsync("api/bookings",new{staffId,serviceId,bookingDate=date,timeSlot=slot,note}); if(!result.IsSuccessStatusCode)return(false,(await result.Content.ReadFromJsonAsync<ApiError>())?.Error??"บันทึกการจองไม่สำเร็จ"); return(true,null); }
    public async Task CancelBooking(int id) { AddAuth(); var result=await http.PostAsync($"api/bookings/{id}/cancel",null); result.EnsureSuccessStatusCode(); }
    public async Task<List<T>> GetAuthorized<T>(string path) { AddAuth(); var result=await http.GetAsync(path); result.EnsureSuccessStatusCode(); return await result.Content.ReadFromJsonAsync<List<T>>() ?? []; }
    public async Task<T> GetAuthorizedObject<T>(string path) { AddAuth(); var result=await http.GetAsync(path); result.EnsureSuccessStatusCode(); return (await result.Content.ReadFromJsonAsync<T>())!; }
    public async Task UpdateKitchen(int id,string status) { AddAuth(); var result=await http.PostAsJsonAsync($"api/kds/{id}/status",new{status}); result.EnsureSuccessStatusCode(); }
    public async Task CloseTable(int id,string method="CASH") { AddAuth(); var result=await http.PostAsJsonAsync($"api/tables/{id}/close",new{method}); result.EnsureSuccessStatusCode(); }
    public async Task MarkBookingPaid(int id,string method="CASH") { AddAuth(); var result=await http.PostAsJsonAsync($"api/bookings/{id}/paid",new{method}); result.EnsureSuccessStatusCode(); }
}
public sealed record MenuItemDto(int Id,string Category,string Name,decimal Price);
public sealed record ServiceDto(int Id,string Name,decimal Price);
public sealed record ArtistDto(int Id,string FullName,string? Bio,string? PortfolioUrl,string[] Specialties);
public sealed record BookingDto(int Id,int CustomerId,int StaffId,int ServiceId,DateTime BookingDate,string TimeSlot,string Status,decimal TotalPrice);
public sealed record AuthResult(string AccessToken,int ExpiresInDays,AuthUser User);
public sealed record AuthUser(int Id,string FullName,string? Email,string? Role);
public sealed record ApiError(string Error);
public sealed record KitchenItemDto(int Id,string Table,string Menu,int Quantity,string KdsStatus,string? Note,DateTime CreatedAt);
public sealed record CashierRow(int TableId,string TableNumber,DateTime? BillRequestedAt,int OrderId,decimal TotalAmount);
public sealed record OverviewDto(int Users,int BookingsToday,int OpenTables,int PendingBookings,decimal PaidToday);
