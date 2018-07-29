using System;

namespace SteamQueryNet.Utils
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EDFAttribute : Attribute
    {
        public EDFAttribute(byte condition) { }
    }
}
