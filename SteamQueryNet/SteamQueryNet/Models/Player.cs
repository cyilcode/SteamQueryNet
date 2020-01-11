using SteamQueryNet.Models.TheShip;
using SteamQueryNet.Attributes;
using System;

namespace SteamQueryNet.Models
{
    public class Player
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
        public int Score { get; set; }

        /// <summary>
        /// Time (in seconds) player has been connected to the server.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Total time as Hours:Minutes:Seconds format.
        /// </summary>
        [NotParsable]
        public string TotalDurationAsString
        {
            get
            {
                TimeSpan totalSpan = TimeSpan.FromSeconds(Duration);
                string parsedHours = totalSpan.Hours >= 10
                    ? totalSpan.Hours.ToString()
                    : $"0{totalSpan.Hours}";

                string parsedMinutes = totalSpan.Minutes >= 10
                    ? totalSpan.Minutes.ToString()
                    : $"0{totalSpan.Minutes}";

                string parsedSeconds = totalSpan.Seconds >= 10
                    ? totalSpan.Seconds.ToString()
                    : $"0{totalSpan.Seconds}";

                return $"{parsedHours}:{parsedMinutes}:{parsedSeconds}";
            }
        }

        /// <summary>
        /// The Ship additional player info.
        /// </summary>
        /// Warning: this property information is not supported by SteamQueryNet yet.
        [ParseCustom]
        public ShipPlayerDetails ShipPlayerDetails { get; set; }
    }
}
