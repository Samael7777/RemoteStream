using System.Net;
using RemoteStream.Client;
using RemoteStream.Protocol;
using RemoteStream.Server;

namespace Test;

internal class Program
{
    static void Main(string[] args)
    {
        using var src = File.OpenRead("test.bin");
        using var dst = File.OpenWrite("dst.bin");

        var ep = new IPEndPoint(IPAddress.Loopback, 7777);

        var serverStream = new RemoteStreamServer(src, ep);

        using var clientStream = new RemoteStreamClient(ep);

        clientStream.CopyTo(dst);

        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }
}