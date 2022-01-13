using SteamQueryNet.Enums;

namespace SteamQueryNet.Models.TheShip
{
	/// <summary>
	/// These fields only exist in a response if the server is running The Ship.
	/// </summary>
	public class ShipGameInfo
	{
		/// <summary>
		/// Indicates the game mode.
		/// </summary>
		public ShipGameMode Mode { get; set; }

		/// <summary>
		/// The number of witnesses necessary to have a player arrested.
		/// </summary>
		public byte Witnesses { get; set; }

		/// <summary>
		/// Time (in seconds) before a player is arrested while being witnessed.
		/// </summary>
		public byte Duration { get; set; }
	}
}
