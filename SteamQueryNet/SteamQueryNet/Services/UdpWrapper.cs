using SteamQueryNet.Interfaces;

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SteamQueryNet.Services
{
    internal sealed class UdpWrapper : IUdpClient
    {
        private readonly UdpClient _udpClient;

        public UdpWrapper(IPEndPoint localIpEndPoint, int sendTimeout, int receiveTimeout)
        {
            _udpClient = new UdpClient(localIpEndPoint);
            _udpClient.Client.SendTimeout = sendTimeout;
            _udpClient.Client.ReceiveTimeout = receiveTimeout;
        }

        public bool IsConnected
        {
            get
            {
                return this._udpClient.Client.Connected;
            }
        }

        public void Close()
        {
            this._udpClient.Close();
        }

        public void Connect(IPEndPoint remoteIpEndpoint)
        {
            this._udpClient.Connect(remoteIpEndpoint);
        }

        public void Dispose()
        {
            this._udpClient.Dispose();
        }

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            return this._udpClient.ReceiveAsync();
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            return this._udpClient.SendAsync(datagram, bytes);
        }
    }
}
