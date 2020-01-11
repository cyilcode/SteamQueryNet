using SteamQueryNet.Models;
using SteamQueryNet.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamQueryNet.Utils;

namespace SteamQueryNet
{
    // This is not really required but imma be a good guy and create this for them people that wants to mock the ServerQuery.
    public interface IServerQuery
    {
        /// <summary>
        /// Renews the server challenge code of the ServerQuery instance in order to be able to execute further operations.
        /// </summary>
        /// <returns>The new created challenge.</returns>
        int RenewChallenge();

        /// <summary>
        /// Renews the server challenge code of the ServerQuery instance in order to be able to execute further operations.
        /// </summary>
        /// <returns>The new created challenge.</returns>
        Task<int> RenewChallengeAsync();

        /// <summary>
        /// Configures and Connects the created instance of SteamQuery UDP socket for Steam Server Query Operations.
        /// </summary>
        /// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
        /// <param name="port">Port of the server that queries will be sent.</param>
        /// <returns>Connected instance of ServerQuery.</returns>
        IServerQuery Connect(string serverAddress, ushort port);

        /// <summary>
        /// Requests and serializes the server information.
        /// </summary>
        /// <returns>Serialized ServerInfo instance.</returns>
        ServerInfo GetServerInfo();

        /// <summary>
        /// Requests and serializes the server information.
        /// </summary>
        /// <returns>Serialized ServerInfo instance.</returns>
        Task<ServerInfo> GetServerInfoAsync();

        /// <summary>
        /// Requests and serializes the list of player information. 
        /// </summary>
        /// <returns>Serialized list of Player instances.</returns>
        List<Player> GetPlayers();

        /// <summary>
        /// Requests and serializes the list of player information. 
        /// </summary>
        /// <returns>Serialized list of Player instances.</returns>
        Task<List<Player>> GetPlayersAsync();

        /// <summary>
        /// Requests and serializes the list of rules defined by the server.
        /// Warning: CS:GO Rules reply is broken since update CSGO 1.32.3.0 (Feb 21, 2014). 
        /// Before the update rules got truncated when exceeding MTU, after the update rules reply is not sent at all.
        /// </summary>
        /// <returns>Serialized list of Rule instances.</returns>
        List<Rule> GetRules();

        /// <summary>
        /// Requests and serializes the list of rules defined by the server.
        /// Warning: CS:GO Rules reply is broken since update CSGO 1.32.3.0 (Feb 21, 2014). 
        /// Before the update rules got truncated when exceeding MTU, after the update rules reply is not sent at all.
        /// </summary>
        /// <returns>Serialized list of Rule instances.</returns>
        Task<List<Rule>> GetRulesAsync();
    }

    public class ServerQuery : IServerQuery, IDisposable
    {
        private IPEndPoint _ipEndpoint;
        private readonly UdpClient _client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));

        private ushort _port;
        private int _currentChallenge;

        /// <summary>
        /// Reflects the udp client connection state.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _client.Client.Connected;
            }
        }

        /// <summary>
        /// Amount of time in miliseconds to terminate send operation if the server won't respond.
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        /// Amount of time in miliseconds to terminate receive operation if the server won't respond.
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// Creates a new instance of ServerQuery without UDP socket connection.
        /// </summary>
        public ServerQuery() { }

        /// <summary>
        /// Creates a new ServerQuery instance for Steam Server Query Operations.
        /// </summary>
        /// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
        /// <param name="port">Port of the server that queries will be sent.</param>
        public ServerQuery(string serverAddress, ushort port)
        {
            PrepareAndConnect(serverAddress, port);
        }

        /// <summary>
        /// Creates a new ServerQuery instance for Steam Server Query Operations.
        /// </summary>
        /// <param name="serverAddressAndPort">IPAddress or HostName of the server and port separated by a colon(:) or a comma(,).</param>
        public ServerQuery(string serverAddressAndPort)
        {
            (string serverAddress, ushort port) = Helpers.ResolveIPAndPortFromString(serverAddressAndPort);
            PrepareAndConnect(serverAddress, port);
        }

        /// <summary>
        /// Creates a new instance of ServerQuery with the given Local IPEndpoint.
        /// </summary>
        /// <param name="customLocalIPEndpoint">Desired local IPEndpoint to bound.</param>
        /// <param name="serverAddressAndPort">IPAddress or HostName of the server and port separated by a colon(:) or a comma(,).</param>
        public ServerQuery(IPEndPoint customLocalIPEndpoint, string serverAddressAndPort)
        {
            _client = new UdpClient(customLocalIPEndpoint);
            (string serverAddress, ushort port) = Helpers.ResolveIPAndPortFromString(serverAddressAndPort);
            PrepareAndConnect(serverAddress, port);
        }

        /// <summary>
        /// Creates a new instance of ServerQuery with the given Local IPEndpoint.
        /// </summary>
        /// <param name="customLocalIPEndpoint">Desired local IPEndpoint to bound.</param>
        /// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
        /// <param name="port">Port of the server that queries will be sent.</param>
        public ServerQuery(IPEndPoint customLocalIPEndpoint, string serverAddress, ushort port)
        {
            _client = new UdpClient(customLocalIPEndpoint);
            PrepareAndConnect(serverAddress, port);
        }

        /// <inheritdoc/>
        public IServerQuery Connect(string serverAddress, ushort port)
        {
            PrepareAndConnect(serverAddress, port);
            return this;
        }

        /// <inheritdoc/>
        public async Task<ServerInfo> GetServerInfoAsync()
        {
            const string requestPayload = "Source Engine Query\0";
            var sInfo = new ServerInfo
            {
                Ping = new Ping().Send(_ipEndpoint.Address).RoundtripTime
            };

            byte[] response = await SendRequestAsync(RequestHeaders.A2S_INFO, Encoding.UTF8.GetBytes(requestPayload));
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
            byte[] response = await SendRequestAsync(RequestHeaders.A2S_PLAYER, BitConverter.GetBytes(-1));
            if (response.Length > 0)
            {
                _currentChallenge = BitConverter.ToInt32(response.Skip(DataResolutionUtils.RESPONSE_CODE_INDEX).Take(sizeof(int)).ToArray(), 0);
            }

            return _currentChallenge;
        }

        /// <inheritdoc/>
        public int RenewChallenge()
        {
            return Helpers.RunSync(RenewChallengeAsync);
        }

        /// <inheritdoc/>
        public async Task<List<Player>> GetPlayersAsync()
        {
            if (_currentChallenge == 0)
            {
                await RenewChallengeAsync();
            }

            byte[] response = await SendRequestAsync(RequestHeaders.A2S_PLAYER, BitConverter.GetBytes(_currentChallenge));
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
            if (_currentChallenge == 0)
            {
                await RenewChallengeAsync();
            }

            byte[] response = await SendRequestAsync(RequestHeaders.A2S_RULES, BitConverter.GetBytes(_currentChallenge));
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
            _client.Close();
            _client.Dispose();
        }

        private void PrepareAndConnect(string serverAddress, ushort port)
        {
            _port = port;

            // Try to parse the serverAddress as IP first
            if (IPAddress.TryParse(serverAddress, out IPAddress parsedIpAddress))
            {
                // Yep its an IP.
                _ipEndpoint = new IPEndPoint(parsedIpAddress, _port);
            }
            else
            {
                // Nope it might be a hostname.
                try
                {
                    IPAddress[] addresslist = Dns.GetHostAddresses(serverAddress);
                    if (addresslist.Length > 0)
                    {
                        // We get the first address.
                        _ipEndpoint = new IPEndPoint(addresslist[0], _port);
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

            _client.Client.SendTimeout = SendTimeout;
            _client.Client.ReceiveTimeout = ReceiveTimeout;
            _client.Connect(_ipEndpoint);
        }

        private async Task<byte[]> SendRequestAsync(byte requestHeader, byte[] payload = null)
        {
            var request = BuildRequest(requestHeader, payload);
            await _client.SendAsync(request, request.Length);
            UdpReceiveResult result = await _client.ReceiveAsync();
            return result.Buffer;
        }

        private byte[] BuildRequest(byte headerCode, byte[] extraParams = null)
        {
            /* All requests consist of 4 FF's followed by a header code to execute the request.
             * Check here: https://developer.valvesoftware.com/wiki/Server_queries#Protocol for further information about the protocol. */
            var request = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, headerCode };

            // If we have any extra payload, concatenate those into our requestHeaders and return;
            return extraParams != null
                ? request.Concat(extraParams).ToArray()
                : request;
        }
    }
}
