using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SkullKingCore.Utility
{
    public static class NetworkUtils
    {
        public static string GetLocalIpHint()
        {
            try
            {
                // Prefer real NICs that are up; grab their IPv4s
                var ipv4s = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up
                                && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                                && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Select(u => u.Address)
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork
                                && !IPAddress.IsLoopback(a)
                                && !a.ToString().StartsWith("169.254.")) // skip APIPA
                    .Select(a => a.ToString())
                    .ToArray();

                // 1) Prefer 192.*; 2) any IPv4
                var ip = ipv4s.FirstOrDefault(x => x.StartsWith("192."))
                         ?? ipv4s.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(ip))
                    return ip;

                // 3) Fallback to hostname (prefer FQDN if resolvable)
                var host = Dns.GetHostName();
                if (!string.IsNullOrWhiteSpace(host))
                {
                    try
                    {
                        var he = Dns.GetHostEntry(host);
                        if (!string.IsNullOrWhiteSpace(he.HostName))
                            return he.HostName; // often FQDN
                    }
                    catch
                    {
                        // ignore and return short host below
                    }
                    return host; // short computer name
                }
            }
            catch
            {
                // ignore and fall through
            }

            // 4) Last resort
            return "127.0.0.1";
        }
    }
}
