using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SteamQueryNet.Utils
{
	internal class Helpers
	{
		internal static TResult RunSync<TResult>(Func<Task<TResult>> func)
		{
			var cultureUi = CultureInfo.CurrentUICulture;
			var culture = CultureInfo.CurrentCulture;
			return new TaskFactory().StartNew(() =>
			{
				Thread.CurrentThread.CurrentCulture = culture;
				Thread.CurrentThread.CurrentUICulture = cultureUi;
				return func();
			}).Unwrap().GetAwaiter().GetResult();
		}

		internal static (string serverAddress, ushort port) ResolveIPAndPortFromString(string serverAddressAndPort)
		{
			const string steamUrl = "steam://connect/";
			// Check for usual suspects.
			if (string.IsNullOrEmpty(serverAddressAndPort))
			{
				throw new ArgumentException($"Couldn't parse hostname or port with value: {serverAddressAndPort}", nameof(serverAddressAndPort));
			}

			// Check if its a steam url.
			if (serverAddressAndPort.Contains(steamUrl))
			{
				// Yep lets get rid of it since we dont need it.
				serverAddressAndPort = serverAddressAndPort.Replace(steamUrl, string.Empty);
			}

			// Lets be a nice guy and clear out all possible copy paste error whitespaces.
			serverAddressAndPort = serverAddressAndPort.Replace(" ", string.Empty);

			// Try with a colon
			string[] parts = serverAddressAndPort.Split(':');
			if (parts.Length != 2)
			{

				// Not a colon. Try a comma then.
				parts = serverAddressAndPort.Split(',');
				if (parts.Length != 2)
				{
					// Y u do dis ?
					throw new ArgumentException($"Couldn't parse hostname or port with value: {serverAddressAndPort}", nameof(serverAddressAndPort));
				}
			}

			// Parse the port see if its in range.
			if (!ushort.TryParse(parts[1], out ushort parsedPort))
			{
				throw new ArgumentException($"Couldn't parse the port number from the parameter with value: {serverAddressAndPort}", nameof(serverAddressAndPort));
			}

			return (parts[0], parsedPort);
		}
	}
}
