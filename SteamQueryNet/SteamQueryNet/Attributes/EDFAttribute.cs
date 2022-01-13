using System;

namespace SteamQueryNet.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	internal sealed class EDFAttribute : Attribute
	{
		internal EDFAttribute(byte condition) { }
	}
}
