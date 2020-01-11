using System;

using Xunit;

namespace SteamQueryNet.Tests
{
    public class ServerQueryTests
    {
        private const string IP_ADDRESS = "54.37.111.216";
        private const int PORT = 27015;

        [Theory]
        [InlineData(IP_ADDRESS)]
        public void ShouldInitializeWithProperHost(string host)
        {
            var squery = new ServerQuery(host, PORT);
            var t = squery.GetPlayers();
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
