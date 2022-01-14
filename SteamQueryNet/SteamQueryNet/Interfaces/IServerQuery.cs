using SteamQueryNet.Models;

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SteamQueryNet.Interfaces
{
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
		Task<int> RenewChallengeAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Configures and Connects the created instance of SteamQuery UDP socket for Steam Server Query Operations.
		/// </summary>
		/// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
		/// <param name="port">Port of the server that queries will be sent.</param>
		/// <returns>Connected instance of ServerQuery.</returns>
		IServerQuery Connect(string serverAddress, ushort port);

		/// <summary>
		/// Configures and Connects the created instance of SteamQuery UDP socket for Steam Server Query Operations.
		/// </summary>
		/// <param name="serverAddressAndPort">IPAddress or HostName of the server and port separated by a colon(:) or a comma(,).</param>
		/// <returns>Connected instance of ServerQuery.</returns>
		IServerQuery Connect(string serverAddressAndPort);

		/// <summary>
		/// Configures and Connects the created instance of SteamQuery UDP socket for Steam Server Query Operations.
		/// </summary>
		/// <param name="customLocalIPEndpoint">Desired local IPEndpoint to bound.</param>
		/// <param name="serverAddressAndPort">IPAddress or HostName of the server and port separated by a colon(:) or a comma(,).</param>
		/// <returns>Connected instance of ServerQuery.</returns>
		IServerQuery Connect(IPEndPoint customLocalIPEndpoint, string serverAddressAndPort);

		/// <summary>
		/// Configures and Connects the created instance of SteamQuery UDP socket for Steam Server Query Operations.
		/// </summary>
		/// <param name="customLocalIPEndpoint">Desired local IPEndpoint to bound.</param>
		/// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
		/// <param name="port">Port of the server that queries will be sent.</param>
		/// <returns>Connected instance of ServerQuery.</returns>
		IServerQuery Connect(IPEndPoint customLocalIPEndpoint, string serverAddress, ushort port);

		/// <summary>
		/// Requests and serializes the server information.
		/// </summary>
		/// <returns>Serialized ServerInfo instance.</returns>
		ServerInfo GetServerInfo();

		/// <summary>
		/// Requests and serializes the server information.
		/// </summary>
		/// <returns>Serialized ServerInfo instance.</returns>
		Task<ServerInfo> GetServerInfoAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Requests and serializes the list of player information. 
		/// </summary>
		/// <returns>Serialized list of Player instances.</returns>
		List<Player> GetPlayers();

		/// <summary>
		/// Requests and serializes the list of player information. 
		/// </summary>
		/// <returns>Serialized list of Player instances.</returns>
		Task<List<Player>> GetPlayersAsync(CancellationToken cancellationToken);

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
		Task<List<Rule>> GetRulesAsync(CancellationToken cancellationToken);
	}
}
