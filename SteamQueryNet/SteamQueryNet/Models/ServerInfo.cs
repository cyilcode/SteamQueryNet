﻿using SteamQueryNet.Attributes;
using SteamQueryNet.Enums;
using SteamQueryNet.Models.TheShip;

namespace SteamQueryNet.Models
{
	public class ServerInfo
	{
		/// <summary>
		/// Protocol version used by the server.
		/// </summary>
		public byte Protocol { get; set; }

		/// <summary>
		/// Name of the server.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Map the server has currently loaded.
		/// </summary>
		public string Map { get; set; }

		/// <summary>
		/// Name of the folder containing the game files.
		/// </summary>
		public string Folder { get; set; }

		/// <summary>
		/// Full name of the game.
		/// </summary>
		public string Game { get; set; }

		/// <summary>
		/// Steam Application ID of game.
		/// </summary>
		public short ID { get; set; }

		/// <summary>
		/// Number of players on the server.
		/// </summary>

		private byte m_players;
		public byte Players
		{
			// Some servers send bots as players. We don't want that here.
			get => (byte)(m_players - Bots);
			set => m_players = value;
		}

		/// <summary>
		/// Maximum number of players the server reports it can hold.
		/// </summary>
		public byte MaxPlayers { get; set; }

		/// <summary>
		/// Number of bots on the server.
		/// </summary>
		public byte Bots { get; set; }

		/// <summary>
		/// Indicates the type of server.
		/// </summary>
		public ServerType ServerType { get; set; }

		/// <summary>
		/// Indicates the operating system of the server.
		/// </summary>
		public ServerEnvironment Environment { get; set; }

		/// <summary>
		/// Indicates whether the server requires a password.
		/// </summary>
		public Visibility Visibility { get; set; }

		/// <summary>
		/// Specifies whether the server uses VAC.
		/// </summary>
		public VAC VAC { get; set; }

		/// <summary>
		/// This property only exist in a response if the server is running The Ship.
		/// Warning: this property information is not supported by SteamQueryNet yet.
		/// </summary>
		[ParseCustom]
		public ShipGameInfo ShipGameInfo { get; set; }

		/// <summary>
		/// Version of the game installed on the server.
		/// </summary>
		public string Version { get; set; }

		/// <summary>
		/// If present, this specifies which additional data fields will be included.
		/// </summary>
		public byte EDF { get; set; }

		/// <summary>
		/// The server's game port number.
		/// </summary>
		[EDF((byte)EDFFlags.Port)]
		public short Port { get; set; }

		/// <summary>
		/// Server's SteamID.
		/// </summary>
		[EDF((byte)EDFFlags.SteamID)]
		public long SteamID { get; set; }

		/// <summary>
		/// Spectator port number for SourceTV.
		/// </summary>
		[EDF((byte)EDFFlags.SourceTVPort)]
		public short SourceTVPort { get; set; }

		/// <summary>
		/// Name of the spectator server for SourceTV.
		/// </summary>
		[EDF((byte)EDFFlags.SourceTVServerName)]
		public string SourceTVServerName { get; set; }

		/// <summary>
		/// Tags that describe the game according to the server (for future use.)
		/// </summary>
		[EDF((byte)EDFFlags.Keywords)]
		public string Keywords { get; set; }

		/// <summary>
		/// The server's 64-bit GameID. If this is present, a more accurate AppID is present in the low 24 bits.
		/// The earlier AppID could have been truncated as it was forced into 16-bit storage.
		/// </summary>
		[EDF((byte)EDFFlags.GameID)]
		public long GameID { get; set; }

		/// <summary>
		/// Calculated roundtrip time of the server.
		/// Warning: this value will be calculated by SteamQueryNet instead of steam itself.
		/// </summary>
		[NotParsable]
		public long Ping { get; set; }
	}
}
