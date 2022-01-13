using SteamQueryNet.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamQueryNet.Utils
{
	internal sealed class RequestHelpers
	{
		internal static byte[] PrepareAS2_INFO_Request(int challenge)
		{
			const string requestPayload = "Source Engine Query\0";
			return BuildRequest(RequestHeaders.A2S_INFO, Encoding.UTF8.GetBytes(requestPayload).Concat(BitConverter.GetBytes(challenge)));
		}

		internal static byte[] PrepareAS2_RENEW_CHALLENGE_Request()
		{
			return BuildRequest(RequestHeaders.A2S_PLAYER, BitConverter.GetBytes(-1));
		}

		internal static byte[] PrepareAS2_GENERIC_Request(byte challengeRequestCode, int challenge)
		{
			return BuildRequest(challengeRequestCode, BitConverter.GetBytes(challenge));
		}

		private static byte[] BuildRequest(byte headerCode, IEnumerable<byte> extraParams = null)
		{
			/* All requests consist of 4 FF's followed by a header code to execute the request.
			 * Check here: https://developer.valvesoftware.com/wiki/Server_queries#Protocol for further information about the protocol. */
			var request = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, headerCode };

			// If we have any extra payload, concatenate those into our requestHeaders and return;
			return extraParams != null
				? request.Concat(extraParams).ToArray()
				: request;
		}
	}
}
