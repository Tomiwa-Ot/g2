using System.Net;

namespace G2.Service.Helper
{
    public class DomainHelper
    {
        public static async Task<List<string>> ResolveToIP(string domain)
        {
            try
            {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(new Uri(domain).Host);
                return [.. addresses.Select(ip => ip.ToString())];
            }
            catch (Exception e)
            {
                Console.WriteLine("Erro " + e.ToString());
                return [];
            }
        }
    }
}