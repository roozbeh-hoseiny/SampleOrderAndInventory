namespace SetupIts.Application.ClientIpContext;
using System.Threading;

public sealed class ClientIpContext : IClientIpContext
{
    private static readonly AsyncLocal<string?> _clientIp = new();

    public string? ClientIp => _clientIp.Value;

    public static void Set(string? ip)
    {
        _clientIp.Value = ip;
    }

    public static void Clear()
    {
        _clientIp.Value = null;
    }
}
