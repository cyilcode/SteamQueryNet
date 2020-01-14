using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SteamQueryNet.Interfaces
{
    public interface IUdpClient : IDisposable
    {
        bool IsConnected { get; }

        void Close();

        void Connect(IPEndPoint remoteIpEndpoint);

        Task<int> SendAsync(byte[] datagram, int bytes);

        Task<UdpReceiveResult> ReceiveAsync();
    }
}
