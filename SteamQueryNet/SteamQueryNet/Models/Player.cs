using SteamQueryNet.Models.TheShip;

namespace SteamQueryNet.Models
{
    public sealed class Player
    {
        /// <summary>
        /// Index of player chunk starting from 0.
        /// </summary>
        public byte Index { get; set; }

        /// <summary>
        /// Name of the player.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Player's score (usually "frags" or "kills".)
        /// </summary>
        public long Score { get; set; }

        /// <summary>
        /// Time (in seconds) player has been connected to the server.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// The Ship additional player info.
        /// </summary>
        public ShipPlayerDetails ShipPlayerDetails { get; set; }
    }
}
