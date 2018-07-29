using System;
using System.Net;
using Xunit;

namespace SteamQueryNet.Tests
{
    public class ServerQueryTests
    {
        private const string IP_ADDRESS = "54.37.111.216";
        private const string HOST_NAME = "surf.wasdzone.com";
        private const int PORT = 27015;

        [Theory]
        [InlineData(IP_ADDRESS)]
        [InlineData(HOST_NAME)]
        public void ShouldInitializeWithProperHost(string host)
        {
            var squery = new ServerQuery(host, PORT);
            squery.GetServerInfo();
        }

        [Theory]
        [InlineData(IPEndPoint.MaxPort + 1)]
        [InlineData(IPEndPoint.MinPort - 1)]
        public void ShouldNotInitializeWithAPortOutOfRange(int port)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var squery = new ServerQuery(IP_ADDRESS, port);
            });
        }

        [Theory]
        [InlineData("256.256.256.256")]
        [InlineData("invalidHost")]
        public void ShouldNotInitializeWithAnInvalidHost(string invalidHost)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var squery = new ServerQuery(invalidHost, PORT);
            });
        }
    }
}
