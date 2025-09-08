using System.Net;

namespace G2.Service.Helper
{
    public class SSRFChecker
    {
        private static List<string> urls = [
            "https://localhost",
            "https://g2hq.live",
            "https://scintillating-crisp-7687f7.netlify.app/"
        ];

        public static async Task<bool> IsUrlSafe(string url)
        {
            try
            {
                Uri uri;
                if (Uri.TryCreate(url, UriKind.Absolute, out uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    if (IsInternalHost(uri.Host)) return false;

                    if (IsHostBlacklisted(uri.Host)) return false;

                    IPAddress[] ipAddresses;

                    ipAddresses = await ResolveToIP(uri.Host);
                    foreach (var ip in ipAddresses)
                    {
                        if (IsPrivateIP(ip))
                            return false;
                    }

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<IPAddress[]> ResolveToIP(string host)
        {
            return await Dns.GetHostAddressesAsync(host);
        }

        private static bool IsPrivateIP(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 127.0.0.0/8 loopback
            if (bytes[0] == 127)
                return true;

            // IPv6 loopback ::1
            if (IPAddress.IsLoopback(ip))
                return true;

            // Link-local (169.254.0.0/16)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            return false;
        }

        private static bool IsInternalHost(string host)
        {
            var lower = host.ToLowerInvariant();

            return lower == "localhost" ||
                lower.EndsWith(".local") ||
                lower.EndsWith(".internal") ||
                lower.EndsWith(".corp") ||
                lower.EndsWith(".intranet");
        }

        private static bool IsHostBlacklisted(string host)
        {
            return urls.Any(url =>
            {
                Uri uri = new Uri(url);
                return uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}