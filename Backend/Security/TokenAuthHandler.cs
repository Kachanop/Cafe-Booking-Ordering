using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using CafeSystem.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CafeSystem.Security;

public sealed class TokenAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IDbContextFactory<CafeDbContext> dbFactory) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        var rawToken = header[7..].Trim();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        await using var db = await dbFactory.CreateDbContextAsync();
        var session = await db.LoginSessions.Include(x => x.User).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.TokenHash == hash && x.ExpiresAt > DateTime.UtcNow);
        if (session is null) return AuthenticateResult.Fail("Invalid or expired token");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, session.User.Id.ToString()), new Claim(ClaimTypes.Name, session.User.FullName), new Claim(ClaimTypes.Role, session.User.Role.Name) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
