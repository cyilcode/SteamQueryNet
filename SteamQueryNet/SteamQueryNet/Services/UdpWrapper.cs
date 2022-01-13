using SteamQueryNet.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SteamQueryNet.Services
{
    internal sealed class UdpWrapper : IUdpClient
    {
        private readonly UdpClient udpClient;
        private readonly int sendTimeout;
        private readonly int receiveTimeout;

        public UdpWrapper(IPEndPoint localIpEndPoint, int sendTimeout, int receiveTimeout)
        {
            udpClient = new UdpClient(localIpEndPoint);
            this.sendTimeout = sendTimeout;
            this.receiveTimeout = receiveTimeout;
        }

        public bool IsConnected
        {
            get
            {
                return udpClient.Client.Connected;
            }
        }

        public void Close()
        {
            udpClient.Close();
        }

        public void Connect(IPEndPoint remoteIpEndpoint)
        {
            udpClient.Connect(remoteIpEndpoint);
        }

        public void Dispose()
        {
            udpClient.Dispose();
        }

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            var asyncResult = udpClient.BeginReceive(null, null);
            asyncResult.AsyncWaitHandle.WaitOne(receiveTimeout);
            if (asyncResult.IsCompleted)
            {
                IPEndPoint remoteEP = null;
                byte[] receivedData = udpClient.EndReceive(asyncResult, ref remoteEP);
                return Task.FromResult(new UdpReceiveResult(receivedData, remoteEP));
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            var asyncResult = udpClient.BeginSend(datagram, bytes, null, null);
            asyncResult.AsyncWaitHandle.WaitOne(sendTimeout);
            if (asyncResult.IsCompleted)
            {
                int num = udpClient.EndSend(asyncResult);
                return Task.FromResult(num);
            }
            else
            {
                throw new TimeoutException();
            }
        }
    }
}
