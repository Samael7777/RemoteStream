using System.Net;

namespace RemoteStream.Client;

public static class IpEndpointEx
{
    public static Uri ToHttpUri(this IPEndPoint ep)
    {
        return BuildUri("http", ep);
    }

    public static Uri ToHttpsUri(this IPEndPoint ep)
    {
        return BuildUri("https", ep);
    }

    private static Uri BuildUri(string scheme, IPEndPoint ep)
    {
        var builder = new UriBuilder()
        {
            Scheme = scheme,
            Host = ep.Address.ToString(),
            Port = ep.Port
        };
        return builder.Uri;
    }
}