namespace SteamQueryNet.Models
{
    public sealed class RequestHeaders
    {
        public const byte A2S_INFO = 0x54;

        public const byte A2S_PLAYER = 0x55;

        public const byte A2S_RULES = 0x56;
    }

    public sealed class ResponseHeaders
    {
        public const byte S2A_INFO = 0x49;

        public const byte S2A_CHALLENGE = 0x41;

        public const byte S2A_PLAYER = 0x44;

        public const byte S2A_RULES = 0x45;
    }
}
