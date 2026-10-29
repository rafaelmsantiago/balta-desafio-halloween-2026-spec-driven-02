using System.Net;
using System.Net.Sockets;

namespace LinkShortener.Api.Application;

// D-07: resolve o host via DNS, sem requisição HTTP ao destino.
public sealed class PublicHostChecker(IHostResolver resolver)
{
    public async Task<bool> IsPubliclyAccessibleAsync(Uri uri, CancellationToken cancellationToken)
    {
        var addresses = await resolver.ResolveAsync(uri.DnsSafeHost, cancellationToken);

        // Basta um endereço não público para recusar o host inteiro.
        return addresses.Count > 0 && addresses.All(IsPublicAddress);
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address))
            return false;

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicIPv4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsPublicIPv6(address),
            _ => false,
        };
    }

    private static bool IsPublicIPv4(byte[] b) => !(
        b[0] == 0                                    // 0.0.0.0/8, "esta rede"
        || b[0] == 10                                // 10.0.0.0/8, privado
        || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) // 100.64.0.0/10, CGNAT
        || b[0] == 127                               // 127.0.0.0/8, loopback
        || (b[0] == 169 && b[1] == 254)              // 169.254.0.0/16, link-local
        || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) // 172.16.0.0/12, privado
        || (b[0] == 192 && b[1] == 0 && b[2] == 0)   // 192.0.0.0/24, reservado IETF
        || (b[0] == 192 && b[1] == 0 && b[2] == 2)   // 192.0.2.0/24, documentação
        || (b[0] == 192 && b[1] == 168)              // 192.168.0.0/16, privado
        || (b[0] == 198 && b[1] is 18 or 19)         // 198.18.0.0/15, benchmark
        || (b[0] == 198 && b[1] == 51 && b[2] == 100) // 198.51.100.0/24, documentação
        || (b[0] == 203 && b[1] == 0 && b[2] == 113) // 203.0.113.0/24, documentação
        || b[0] >= 224);                             // multicast, reservado e broadcast

    private static bool IsPublicIPv6(IPAddress address)
    {
        if (address.Equals(IPAddress.IPv6None)
            || address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6UniqueLocal
            || address.IsIPv6Multicast)
            return false;

        var b = address.GetAddressBytes();

        // 2001:db8::/32, documentação.
        return !(b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0d && b[3] == 0xb8);
    }
}
