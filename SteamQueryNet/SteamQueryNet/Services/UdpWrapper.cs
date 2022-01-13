using SteamQueryNet.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
			m_sendTimeout = sendTimeout;
			m_receiveTimeout = receiveTimeout;
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

		public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
		{
			var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			source.CancelAfter(m_receiveTimeout);

			try
			{
				return await m_udpClient.ReceiveAsync(source.Token);
			}
			catch (OperationCanceledException)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					throw;
				}
				else
				{
					throw new TimeoutException();
				}
			}
		}

		public async Task<int> SendAsync(byte[] datagram, CancellationToken cancellationToken)
		{
			var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			source.CancelAfter(m_receiveTimeout);

			try
			{
				return await m_udpClient.SendAsync(datagram, source.Token);
			}
			catch (OperationCanceledException)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					throw;
				}
				else
				{
					throw new TimeoutException();
				}
			}
		}
	}
}
