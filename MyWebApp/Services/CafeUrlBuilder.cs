using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MyWebApp.Services;

public static class CafeUrlBuilder
{
    public static string GetBaseUrl(HttpRequest request, IConfiguration configuration)
    {
        var configured = configuration["Cafe:PublicBaseUrl"]?.TrimEnd('/');
        if (Uri.TryCreate(configured, UriKind.Absolute, out var publicUri))
            return publicUri.GetLeftPart(UriPartial.Authority);

        var host = request.Host;
        if (IPAddress.TryParse(host.Host, out var address) && IPAddress.IsLoopback(address) ||
            string.Equals(host.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            var localAddress = GetLanAddress();
            if (localAddress is not null) host = new HostString(localAddress, host.Port ?? (request.IsHttps ? 443 : 80));
        }

        return $"{request.Scheme}://{host}";
    }

    private static string? GetLanAddress()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up &&
                    adapter.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
                .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                .Select(unicast => unicast.Address)
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip) && !ip.ToString().StartsWith("169.254.", StringComparison.Ordinal))?.ToString();
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }
}
