using Moq;
using System;
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
        private readonly string IP_AND_PORT_COLON = $"{IP_ADDRESS}:{PORT}";
        private readonly string IP_AND_PORT_COMMA = $"{IP_ADDRESS},{PORT}";
        private readonly string HOST_NAME_AND_PORT_COLON = $"{HOST_NAME}:{PORT}";
        private readonly string HOST_NAME_AND_PORT_COMMA = $"{HOST_NAME},{PORT}";

        [Theory]
        [InlineData(IP_ADDRESS)]
        [InlineData(HOST_NAME)]
        public void ShouldInitializeWithProperHost(string host)
        {
            var client = new Mock<UdpClient>();
            client.Setup(x => x.Connect(It.IsAny<IPEndPoint>()));
            new ServerQuery(client.Object, It.IsAny<IPEndPoint>()).Connect(host, PORT);
        }

        // Please don't hate me but i think ClassDataAttribute sucks for many reasons.
        [Fact]
        public void ShouldInitializeWithProperHostAndPort()
        {
            new ServerQuery(IP_AND_PORT_COLON);
            new ServerQuery(IP_AND_PORT_COMMA);
            new ServerQuery(HOST_NAME_AND_PORT_COLON);
            new ServerQuery(HOST_NAME_AND_PORT_COMMA);
        }

        [Theory]
        [InlineData("256.256.256.256")]
        [InlineData("invalidHost")]
        [InlineData("invalidHost:-1")]
        [InlineData("invalidHost,-1")]
        [InlineData("invalidHost:65536")]
        [InlineData("invalidHost,65536")]
        [InlineData("256.256.256.256:-1")]
        [InlineData("256.256.256.256,-1")]
        [InlineData("256.256.256.256:65536")]
        [InlineData("256.256.256.256,65536")]
        public void ShouldNotInitializeWithAnInvalidHost(string invalidHost)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var squery = new ServerQuery(invalidHost, PORT);
            });
        }
    }
}
