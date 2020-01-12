using Moq;

using SteamQueryNet.Interfaces;
using SteamQueryNet.Models;
using SteamQueryNet.Tests.Responses;
using SteamQueryNet.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace SteamQueryNet.Tests
{
    public class ServerQueryTests
    {
        private const string IP_ADDRESS = "127.0.0.1";
        private const string HOST_NAME = "localhost";
        private const ushort PORT = 27015;
        private readonly IPEndPoint _localIpEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);

        [Theory]
        [InlineData(IP_ADDRESS)]
        [InlineData(HOST_NAME)]
        public void ShouldInitializeWithProperHost(string host)
        {
            using (var sq = new ServerQuery(new Mock<IUdpClient>().Object, It.IsAny<IPEndPoint>()))
            {
                sq.Connect(host, PORT);
            }
        }

        [Theory]
        [InlineData("127.0.0.1:27015")]
        [InlineData("127.0.0.1,27015")]
        [InlineData("localhost:27015")]
        [InlineData("localhost,27015")]
        [InlineData("steam://connect/localhost:27015")]
        [InlineData("steam://connect/127.0.0.1:27015")]
        public void ShouldInitializeWithProperHostAndPort(string ipAndHost)
        {
            using (var sq = new ServerQuery(new Mock<IUdpClient>().Object, It.IsAny<IPEndPoint>()))
            {
                sq.Connect(ipAndHost);
            }
        }

        [Theory]
        [InlineData("invalidHost:-1")]
        [InlineData("invalidHost,-1")]
        [InlineData("invalidHost:65536")]
        [InlineData("invalidHost,65536")]
        [InlineData("256.256.256.256:-1")]
        [InlineData("256.256.256.256,-1")]
        [InlineData("256.256.256.256:65536")]
        [InlineData("256.256.256.256,65536")]
        public void ShouldNotInitializeWithAnInvalidHostAndPort(string invalidHost)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                using (var sq = new ServerQuery(new Mock<IUdpClient>().Object, It.IsAny<IPEndPoint>()))
                {
                    sq.Connect(invalidHost);
                }
            });
        }

        [Fact]
        public void GetServerInfo_ShouldPopulateCorrectServerInfo()
        {
            (byte[] udpPacket, object responseObject) = ResponseHelper.GetValidResponse(ResponseHelper.ServerInfo);
            var expectedObject = (ServerInfo)responseObject;

            byte[] requestPacket = RequestHelpers.PrepareAS2_INFO_Request();
            Mock<IUdpClient> udpClientMock = SetupReceiveResponse(udpPacket);
            SetupRequestCompare(requestPacket, udpClientMock);

            // Ayylmao it looks ugly as hell but we will improve it later on.
            using (var sq = new ServerQuery(udpClientMock.Object, _localIpEndpoint))
            {
                ServerInfo result = sq.GetServerInfo();
                Assert.Equal(expectedObject.Bots, result.Bots);
                Assert.Equal(expectedObject.EDF, result.EDF);
                Assert.Equal(expectedObject.Environment, result.Environment);
                Assert.Equal(expectedObject.Folder, result.Folder);
                Assert.Equal(expectedObject.Game, result.Game);
                Assert.Equal(expectedObject.GameID, result.GameID);
                Assert.Equal(expectedObject.ID, result.ID);
                Assert.Equal(expectedObject.Keywords, result.Keywords);
                Assert.Equal(expectedObject.Map, result.Map);
                Assert.Equal(expectedObject.MaxPlayers, result.MaxPlayers);
                Assert.Equal(expectedObject.Name, result.Name);
                Assert.Equal(expectedObject.Ping, result.Ping);
                Assert.Equal(expectedObject.Players, result.Players);
                Assert.Equal(expectedObject.Port, result.Port);
                Assert.Equal(expectedObject.Protocol, result.Protocol);
                Assert.Equal(expectedObject.ServerType, result.ServerType);
                Assert.Equal(expectedObject.ShipGameInfo, result.ShipGameInfo);
                Assert.Equal(expectedObject.SourceTVPort, result.SourceTVPort);
                Assert.Equal(expectedObject.SourceTVServerName, result.SourceTVServerName);
                Assert.Equal(expectedObject.SteamID, result.SteamID);
                Assert.Equal(expectedObject.VAC, result.VAC);
                Assert.Equal(expectedObject.Version, result.Version);
                Assert.Equal(expectedObject.Visibility, result.Visibility);
            }
        }

        [Fact(Skip = "Not Completed Yet")]
        public void GetPlayers_ShouldPopulateCorrectPlayers()
        {
            (byte[] udpPacket, object responseObject) = ResponseHelper.GetValidResponse(ResponseHelper.GetPlayers);
            var expectedObject = (List<Player>)responseObject;

            byte[] requestPacket = RequestHelpers.PrepareAS2_RENEW_CHALLENGE_Request();
            Mock<IUdpClient> udpClientMock = SetupReceiveResponse(udpPacket);
            SetupRequestCompare(requestPacket, udpClientMock);

            // Ayylmao it looks ugly as hell but we will improve it later on.
            using (var sq = new ServerQuery(udpClientMock.Object, _localIpEndpoint))
            {
                List<Player> result = sq.GetPlayers();

            }
        }

        private void SetupRequestCompare(byte[] requestPacket, Mock<IUdpClient> udpClientMock)
        {
            udpClientMock
                .Setup(x => x.SendAsync(It.IsAny<byte[]>(), requestPacket.Length))
                .Callback<byte[], int>((request, length) =>
                {
                    Assert.True(TestValidators.CompareBytes(requestPacket, request));
                });
        }

        private Mock<IUdpClient> SetupReceiveResponse(byte[] udpPacket)
        {
            Mock<IUdpClient> udpClientMock = new Mock<IUdpClient>();
            UdpReceiveResult result = new UdpReceiveResult(udpPacket, _localIpEndpoint);
            udpClientMock.Setup(x => x.ReceiveAsync()).ReturnsAsync(result);
            return udpClientMock;
        }
    }
}
