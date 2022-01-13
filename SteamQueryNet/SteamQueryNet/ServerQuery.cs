using SteamQueryNet.Interfaces;
using SteamQueryNet.Models;
using SteamQueryNet.Services;
using SteamQueryNet.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SteamQueryNet.Tests")]
namespace SteamQueryNet
{
	public class ServerQuery : IServerQuery, IDisposable
	{
		private IPEndPoint m_remoteIpEndpoint;

		private ushort m_port;
		private int m_currentChallenge;

		internal virtual IUdpClient UdpClient { get; private set; }

		/// <summary>
		/// Reflects the udp client connection state.
		/// </summary>
		public bool IsConnected => UdpClient.IsConnected;

		/// <summary>
		/// Amount of time in milliseconds to terminate send operation if the server won't respond.
		/// </summary>
		public int SendTimeout { get; set; }

		/// <summary>
		/// Amount of time in milliseconds to terminate receive operation if the server won't respond.
		/// </summary>
		public int ReceiveTimeout { get; set; }

		/// <summary>
		/// Creates a new instance of ServerQuery with given UDPClient and remote endpoint.
		/// </summary>
		/// <param name="udpClient">UdpClient to communicate.</param>
		/// <param name="remoteEndpoint">Remote server endpoint.</param>
		public ServerQuery(IUdpClient udpClient, IPEndPoint remoteEndpoint)
		{
			UdpClient = udpClient;
			m_remoteIpEndpoint = remoteEndpoint;
		}

		/// <summary>
		/// Creates a new instance of ServerQuery without UDP socket connection.
		/// </summary>
		public ServerQuery() { }

		/// <summary>
		/// Creates a new ServerQuery instance for Steam Server Query Operations.
		/// </summary>
		/// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
		/// <param name="port">Port of the server that queries will be sent.</param>
		public IServerQuery Connect(string serverAddress, ushort port)
		{
			PrepareAndConnect(serverAddress, port);
			return this;
		}

		/// <summary>
		/// Creates a new ServerQuery instance for Steam Server Query Operations.
		/// </summary>
		/// <param name="serverAddressAndPort">IPAddress or HostName of the server and port separated by a colon(:) or a comma(,).</param>
		public IServerQuery Connect(string serverAddressAndPort)
		{
			(string serverAddress, ushort port) = Helpers.ResolveIPAndPortFromString(serverAddressAndPort);
			PrepareAndConnect(serverAddress, port);
			return this;
		}

		/// <summary>
		/// Creates a new instance of ServerQuery with the given Local IPEndpoint.
		/// </summary>
		/// <param name="customLocalIPEndpoint">Desired local IPEndpoint to bound.</param>
		/// <param name="serverAddressAndPort">IPAddress or HostName of the server and port separated by a colon(:) or a comma(,).</param>
		public IServerQuery Connect(IPEndPoint customLocalIPEndpoint, string serverAddressAndPort)
		{
			UdpClient = new UdpWrapper(customLocalIPEndpoint, SendTimeout, ReceiveTimeout);
			(string serverAddress, ushort port) = Helpers.ResolveIPAndPortFromString(serverAddressAndPort);
			PrepareAndConnect(serverAddress, port);
			return this;
		}

		/// <summary>
		/// Creates a new instance of ServerQuery with the given Local IPEndpoint.
		/// </summary>
		/// <param name="customLocalIPEndpoint">Desired local IPEndpoint to bound.</param>
		/// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
		/// <param name="port">Port of the server that queries will be sent.</param>
		public IServerQuery Connect(IPEndPoint customLocalIPEndpoint, string serverAddress, ushort port)
		{
			UdpClient = new UdpWrapper(customLocalIPEndpoint, SendTimeout, ReceiveTimeout);
			PrepareAndConnect(serverAddress, port);
			return this;
		}

		/// <inheritdoc/>
		public async Task<ServerInfo> GetServerInfoAsync()
		{
			var sInfo = new ServerInfo
			{
				Ping = new Ping().Send(m_remoteIpEndpoint.Address)?.RoundtripTime ?? default
			};

			if (m_currentChallenge == 0)
			{
				await RenewChallengeAsync();
			}

			byte[] response = await SendRequestAsync(RequestHelpers.PrepareAS2_INFO_Request(m_currentChallenge));
			if (response.Length > 0)
			{
				DataResolutionUtils.ExtractData(sInfo, response, nameof(sInfo.EDF), true);
			}

			return sInfo;
		}

		/// <inheritdoc/>
		public ServerInfo GetServerInfo()
		{
			return Helpers.RunSync(GetServerInfoAsync);
		}

		/// <inheritdoc/>
		public async Task<int> RenewChallengeAsync()
		{
			byte[] response = await SendRequestAsync(RequestHelpers.PrepareAS2_RENEW_CHALLENGE_Request());
			if (response.Length > 0)
			{
				m_currentChallenge = BitConverter.ToInt32(response.Skip(DataResolutionUtils.RESPONSE_CODE_INDEX).Take(sizeof(int)).ToArray(), 0);
			}

			return m_currentChallenge;
		}

		/// <inheritdoc/>
		public int RenewChallenge()
		{
			return Helpers.RunSync(RenewChallengeAsync);
		}

		/// <inheritdoc/>
		public async Task<List<Player>> GetPlayersAsync()
		{
			if (m_currentChallenge == 0)
			{
				await RenewChallengeAsync();
			}

			byte[] response = await SendRequestAsync(
				RequestHelpers.PrepareAS2_GENERIC_Request(RequestHeaders.A2S_PLAYER, m_currentChallenge));

			if (response.Length > 0)
			{
				return DataResolutionUtils.ExtractListData<Player>(response);
			}
			else
			{
				throw new InvalidOperationException("Server did not response the query");
			}
		}

		/// <inheritdoc/>
		public List<Player> GetPlayers()
		{
			return Helpers.RunSync(GetPlayersAsync);
		}

		/// <inheritdoc/>
		public async Task<List<Rule>> GetRulesAsync()
		{
			if (m_currentChallenge == 0)
			{
				await RenewChallengeAsync();
			}

			byte[] response = await SendRequestAsync(
				RequestHelpers.PrepareAS2_GENERIC_Request(RequestHeaders.A2S_RULES, m_currentChallenge));

			if (response.Length > 0)
			{
				return DataResolutionUtils.ExtractListData<Rule>(response);
			}
			else
			{
				throw new InvalidOperationException("Server did not response the query");
			}
		}

		/// <inheritdoc/>
		public List<Rule> GetRules()
		{
			return Helpers.RunSync(GetRulesAsync);
		}

		/// <summary>
		/// Disposes the object and its disposables.
		/// </summary>
		public void Dispose()
		{
			UdpClient.Close();
			UdpClient.Dispose();
		}

		private void PrepareAndConnect(string serverAddress, ushort port)
		{
			m_port = port;

			// Try to parse the serverAddress as IP first
			if (IPAddress.TryParse(serverAddress, out IPAddress parsedIpAddress))
			{
				// Yep its an IP.
				m_remoteIpEndpoint = new IPEndPoint(parsedIpAddress, m_port);
			}
			else
			{
				// Nope it might be a hostname.
				try
				{
					IPAddress[] addressList = Dns.GetHostAddresses(serverAddress);
					if (addressList.Length > 0)
					{
						// We get the first address.
						m_remoteIpEndpoint = new IPEndPoint(addressList[0], m_port);
					}
					else
					{
						throw new ArgumentException($"Invalid host address {serverAddress}");
					}
				}
				catch (SocketException ex)
				{
					throw new ArgumentException("Could not reach the hostname.", ex);
				}
			}

			UdpClient ??= new UdpWrapper(new IPEndPoint(IPAddress.Any, 0), SendTimeout, ReceiveTimeout);
			UdpClient.Connect(m_remoteIpEndpoint);
		}

		private async Task<byte[]> SendRequestAsync(byte[] request)
		{
			await UdpClient.SendAsync(request, request.Length);
			UdpReceiveResult result = await UdpClient.ReceiveAsync();
			return result.Buffer;
		}
	}
}
