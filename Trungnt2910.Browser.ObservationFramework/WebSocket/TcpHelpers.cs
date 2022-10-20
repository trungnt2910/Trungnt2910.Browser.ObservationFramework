using System.Net.Sockets;
using System.Net;

namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

internal static class TcpHelpers
{
    public static int FindUnusedPort()
    {
        int port = 0;
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            var localEP = new IPEndPoint(IPAddress.Any, 0);
            socket.Bind(localEP);
            localEP = (IPEndPoint)socket.LocalEndPoint!;
            port = localEP.Port;
        }
        finally
        {
            socket.Close();
        }
        return port;
    }
}
