using SteamQueryNet.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SteamQueryNet.Services
{
	internal sealed class UdpWrapper : IUdpClient
	{
		private readonly UdpClient m_udpClient;
		private readonly int m_sendTimeout;
		private readonly int m_receiveTimeout;

		public UdpWrapper(IPEndPoint localIpEndPoint, int sendTimeout, int receiveTimeout)
		{
			m_udpClient = new UdpClient(localIpEndPoint);
			this.m_sendTimeout = sendTimeout;
			this.m_receiveTimeout = receiveTimeout;
		}

		public bool IsConnected => m_udpClient.Client.Connected;

		public void Close()
		{
			m_udpClient.Close();
		}

		public void Connect(IPEndPoint remoteIpEndpoint)
		{
			m_udpClient.Connect(remoteIpEndpoint);
		}

		public void Dispose()
		{
			m_udpClient.Dispose();
		}

		public Task<UdpReceiveResult> ReceiveAsync()
		{
			var asyncResult = m_udpClient.BeginReceive(null, null);
			asyncResult.AsyncWaitHandle.WaitOne(m_receiveTimeout);
			if (asyncResult.IsCompleted)
			{
				IPEndPoint remoteEP = null;
				byte[] receivedData = m_udpClient.EndReceive(asyncResult, ref remoteEP);
				return Task.FromResult(new UdpReceiveResult(receivedData, remoteEP));
			}
			else
			{
				throw new TimeoutException();
			}
		}

		public Task<int> SendAsync(byte[] datagram, int bytes)
		{
			var asyncResult = m_udpClient.BeginSend(datagram, bytes, null, null);
			asyncResult.AsyncWaitHandle.WaitOne(m_sendTimeout);
			if (asyncResult.IsCompleted)
			{
				int num = m_udpClient.EndSend(asyncResult);
				return Task.FromResult(num);
			}
			else
			{
				throw new TimeoutException();
			}
		}
	}
}
