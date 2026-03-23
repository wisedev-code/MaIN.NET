using System.Net.Sockets;

namespace MaIN.Core.E2ETests.Helpers;

public static class NetworkHelper
{
    public static bool PingHost(string host, int port, int timeout)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));

            if (!success)
            {
                return false;
            }

            client.EndConnect(result);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
