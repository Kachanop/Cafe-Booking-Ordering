using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyWebApp.Models;

namespace MyWebApp.Services;

public sealed class CafeBackendClient(HttpClient http, IHttpContextAccessor accessor)
{
    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        var token = accessor.HttpContext?.Session.GetString("Cafe.Token");
        if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<(T? Value, string? Error, HttpStatusCode Status)> Get<T>(string path)
    {
        using var request = Request(HttpMethod.Get, path);
        try { using var response = await http.SendAsync(request); var payload = await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) return (default, ReadError(payload), response.StatusCode); return (JsonSerializer.Deserialize<T>(payload, JsonOptions), null, response.StatusCode); }
        catch (HttpRequestException) { return (default, "ติดต่อ Backend ไม่ได้ กรุณาตรวจสอบว่า Backend ทำงานอยู่", HttpStatusCode.ServiceUnavailable); }
    }

    public async Task<(T? Value, string? Error, HttpStatusCode Status)> Send<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = Request(method, path); if (body is not null) request.Content = JsonContent.Create(body);
        try { using var response = await http.SendAsync(request); var payload = response.Content is null ? "" : await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) return (default, ReadError(payload), response.StatusCode); if (typeof(T) == typeof(bool)) return ((T)(object)true, null, response.StatusCode); if (string.IsNullOrWhiteSpace(payload)) return (default, null, response.StatusCode); return (JsonSerializer.Deserialize<T>(payload, JsonOptions), null, response.StatusCode); }
        catch (HttpRequestException) { return (default, "ติดต่อ Backend ไม่ได้ กรุณาตรวจสอบว่า Backend ทำงานอยู่", HttpStatusCode.ServiceUnavailable); }
    }

    public async Task<(bool Ok, string? Error)> Send(string method, string path, object? body = null)
    {
        using var request = Request(new HttpMethod(method), path); if (body is not null) request.Content = JsonContent.Create(body);
        try { using var response = await http.SendAsync(request); var payload = await response.Content.ReadAsStringAsync(); return response.IsSuccessStatusCode ? (true, null) : (false, ReadError(payload)); }
        catch (HttpRequestException) { return (false, "ติดต่อ Backend ไม่ได้ กรุณาตรวจสอบว่า Backend ทำงานอยู่"); }
    }

    private static string? ReadError(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return "คำขอไม่สำเร็จ กรุณาตรวจสอบข้อมูลและสิทธิ์";
        try { return JsonDocument.Parse(payload).RootElement.TryGetProperty("error", out var error) ? error.GetString() : "คำขอไม่สำเร็จ กรุณาตรวจสอบข้อมูลและสิทธิ์"; }
        catch { return "คำขอไม่สำเร็จ กรุณาตรวจสอบข้อมูลและสิทธิ์"; }
    }
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
