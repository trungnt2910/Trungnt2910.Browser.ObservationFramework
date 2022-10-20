using System.Net.Sockets;
using System.Net;

namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

internal static class TcpHelpers
{
    public static int FindUnusedPort()
    {
        //get an empty port
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
