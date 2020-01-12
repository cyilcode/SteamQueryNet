using System.Linq;

namespace SteamQueryNet.Tests
{
    internal static class TestValidators
    {
        public static bool CompareBytes(byte[] source1, byte[] source2)
        {
            // Lets check their refs first.
            if (source1 == source2)
            {
                // yay.
                return true;
            }

            // Check their operability.
            if (source1 == null || source2 == null)
            {
                // Consider: Maybe we should throw an exception here.
                return false;
            }

            // They are not even same length lul.
            if (source1.Length != source1.Length)
            {
                return false;
            }

            // Byte by byte comparison intensifies.
            for (int i = 0; i < source1.Length; ++i)
            {
                if (source1[i] != source2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
