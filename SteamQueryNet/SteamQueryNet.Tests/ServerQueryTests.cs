using Moq;

using Newtonsoft.Json;

using SteamQueryNet.Interfaces;
using SteamQueryNet.Models;
using SteamQueryNet.Tests.Responses;
using SteamQueryNet.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace SteamQueryNet.Tests
{
	public class ServerQueryTests
	{
		private const string IP_ADDRESS = "127.0.0.1";
		private const string HOST_NAME = "localhost";
		private const ushort PORT = 27015;
		private byte _packetCount = 0;
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
			(byte[] responsePacket, object responseObject) = ResponseHelper.GetValidResponse(ResponseHelper.ServerInfo);
			var expectedObject = (ServerInfo)responseObject;

			byte[][] requestPackets = new byte[][] { RequestHelpers.PrepareAS2_INFO_Request(0) };
			byte[][] responsePackets = new byte[][] { responsePacket };

			Mock<IUdpClient> udpClientMock = SetupReceiveResponse(responsePackets);
			SetupRequestCompare(requestPackets, udpClientMock);

			using (var sq = new ServerQuery(udpClientMock.Object, _localIpEndpoint))
			{
				Assert.Equal(JsonConvert.SerializeObject(expectedObject), JsonConvert.SerializeObject(sq.GetServerInfo()));
			}
		}

		[Fact]
		public void GetPlayers_ShouldPopulateCorrectPlayers()
		{
			(byte[] playersPacket, object responseObject) = ResponseHelper.GetValidResponse(ResponseHelper.GetPlayers);
			var expectedObject = (List<Player>)responseObject;

			byte[] challengePacket = RequestHelpers.PrepareAS2_RENEW_CHALLENGE_Request();

			// Both requests will be executed on AS2_PLAYER since thats how you refresh challenges.
			byte[][] requestPackets = new byte[][] { challengePacket, challengePacket };

			// First response is the Challenge renewal response and the second 
			byte[][] responsePackets = new byte[][] { challengePacket, playersPacket };

			Mock<IUdpClient> udpClientMock = SetupReceiveResponse(responsePackets);
			SetupRequestCompare(requestPackets, udpClientMock);

			// Ayylmao it looks ugly as hell but we will improve it later on.
			using (var sq = new ServerQuery(udpClientMock.Object, _localIpEndpoint))
			{
				Assert.Equal(JsonConvert.SerializeObject(expectedObject), JsonConvert.SerializeObject(sq.GetPlayers()));
			}
		}

		/*
		 * We keep this test here to be able to have us a notifier when the Rules API becomes available.
		 * So, this is more like an integration test than an unit test.
		 * If this test starts to fail, we'll know that the Rules API started to respond.
		 */
		[Fact]
		public void GetRules_ShouldThrowTimeoutException()
		{
			// Surf Heaven rulez.
			const string trustedServer = "steam://connect/54.37.111.217:27015";

			using (var sq = new ServerQuery())
			{
				sq.Connect(trustedServer);

				// Make sure that the server is still alive.
				Assert.True(sq.IsConnected);
				bool responded = Task.WaitAll(new Task[] { sq.GetRulesAsync() }, 2000);
				Assert.True(!responded);
			}
		}

		private void SetupRequestCompare(IEnumerable<byte[]> requestPackets, Mock<IUdpClient> udpClientMock)
		{
			udpClientMock
				.Setup(x => x.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
				.Callback<byte[], int>((request, length) =>
				{
					Assert.True(TestValidators.CompareBytes(requestPackets.ElementAt(_packetCount), request));
					++_packetCount;
				});
		}

		private Mock<IUdpClient> SetupReceiveResponse(IEnumerable<byte[]> udpPackets)
		{
			var udpClientMock = new Mock<IUdpClient>();
			var setupSequence = udpClientMock.SetupSequence(x => x.ReceiveAsync());
			foreach (byte[] packet in udpPackets)
			{
				setupSequence = setupSequence.ReturnsAsync(new UdpReceiveResult(packet, _localIpEndpoint));
			}

			return udpClientMock;
		}
	}
}
