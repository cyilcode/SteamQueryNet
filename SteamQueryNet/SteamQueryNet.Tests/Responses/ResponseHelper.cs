using SteamQueryNet.Enums;
using SteamQueryNet.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SteamQueryNet.Tests.Responses
{
    internal sealed class ResponseHelper
    {
        internal const string ServerInfo = "/Responses/ServerInfoValidResponse.txt";
        internal const string GetPlayers = "/Responses/GetPlayersValidResponse.txt";

        // Decided to use a response from SurfHeaven servers and cache them. Got administrators approval.
        private static readonly Dictionary<string, object> _responses = new Dictionary<string, object>()
        {
            {
                ServerInfo, new ServerInfo
                {
                    Bots = 3,
                    EDF = 177,
                    Environment = ServerEnvironment.Linux,
                    Folder = "csgo",
                    Game = "Counter-Strike: Global Offensive",
                    GameID = 730,
                    ID = 730,
                    Keywords = "!knife,!ws,64tick,SurfHeaven,autobhop,cs20,knife,rank,skins,stats,surf,surfing,surftimer,timer,ws,secure",
                    Map = "surf_sinister_evil",
                    MaxPlayers = 40,
                    Name = " SurfHeaven #5 Top 250/VIP",
                    Ping = 0,
                    Players = 4,
                    Port = 27015,
                    Protocol = 17,
                    ServerType = ServerType.Dedicated,
                    ShipGameInfo = null,
                    SourceTVPort = 0,
                    SourceTVServerName = null,
                    SteamID = 85568392920053114,
                    VAC = VAC.Secured,
                    Version = "1.37.3.6",
                    Visibility = Visibility.Public
                }
            },
            { 
                GetPlayers, new List<Player>()
                { 
                    new Player 
                    {
                        Duration = 26078.668f,
                        Index = 0,
                        Name = ">:( ٠ Mr.PogoMogoFogoNogo 01:3",
                        Score = 0
                    },
                    new Player
                    {
                        Duration = 2209.14038f,
                        Index = 0,
                        Name = "FuMe JF",
                        Score = 0
                    },
                    new Player
                    {
                        Duration = 2040.9375f,
                        Index = 0,
                        Name = "30SanchezZ",
                        Score = 0
                    },
                    new Player
                    {
                        Duration = 722.6248f,
                        Index = 0,
                        Name = "kzz--",
                        Score = 0
                    }
                } 
            }
        };

        public static (byte[], object) GetValidResponse(string responseType)
        {
            string validResponseString = File.ReadAllText(Environment.CurrentDirectory + responseType);
            if (!_responses.TryGetValue(responseType, out object responseObject))
            {
                throw new ArgumentException($"Invalid response type received: {responseType}." +
                    $"Consider registering {responseType} into ResponseHelpers dictionary.", nameof(responseType));
            }

            return (validResponseString.Split(',').Select(x => byte.Parse(x)).ToArray(), responseObject);
        }
    }
}
