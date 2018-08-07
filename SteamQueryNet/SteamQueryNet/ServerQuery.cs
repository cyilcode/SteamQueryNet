using SteamQueryNet.Models;
using SteamQueryNet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SteamQueryNet
{
    // This is not really required but imma be a good guy and create this for them people that wants to mock the ServerQuery.
    public interface IServerQuery
    {
        /// <summary>
        /// Renews the server challenge code of the ServerQuery instance in order to be able to execute further operations.
        /// </summary>
        void RenewChallenge();

        /// <summary>
        /// Configures and Connects the created instance of SteamQuery UDP socket for Steam Server Query Operations.
        /// </summary>
        /// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
        /// <param name="port">Port of the server that queries will be sent.</param>
        void Connect(string serverAddress, int port);

        /// <summary>
        /// Requests and serializes the server information.
        /// </summary>
        /// <returns>Serialized ServerInfo instance.</returns>
        ServerInfo GetServerInfo();

        /// <summary>
        /// Requests and serializes the list of player information. 
        /// </summary>
        /// <returns>Serialized list of Player instances.</returns>
        List<Player> GetPlayers();

        /// <summary>
        /// Requests and serializes the list of rules defined by the server.
        /// Warning: CS:GO Rules reply is broken since update CSGO 1.32.3.0 (Feb 21, 2014). 
        /// Before the update rules got truncated when exceeding MTU, after the update rules reply is not sent at all.
        /// </summary>
        /// <returns>Serialized list of Rule instances.</returns>
        List<Rule> GetRules();
    }

    public class ServerQuery : IServerQuery, IDisposable
    {
        private const int RESPONSE_HEADER_COUNT = 6;
        private const int RESPONSE_CODE_INDEX = 5;

        private readonly UdpClient _client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        private IPEndPoint _ipEndpoint;

        private int _port;
        private int _currentChallenge;

        /// <summary>
        /// Reflects the udp client connection state.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _client.Client.Connected;
            }
        }

        /// <summary>
        /// Amount of time in miliseconds to terminate send operation if the server won't respond.
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        /// Amount of time in miliseconds to terminate receive operation if the server won't respond.
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// Creates a new instance of ServerQuery without UDP socket connection.
        /// </summary>
        public ServerQuery() { }

        /// <summary>
        /// Creates a new ServerQuery instance for Steam Server Query Operations.
        /// </summary>
        /// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
        /// <param name="port">Port of the server that queries will be sent.</param>
        public ServerQuery(string serverAddress, int port)
        {
            PrepareAndConnect(serverAddress, port);
        }

        /// <inheritdoc/>
        public void Connect(string serverAddress, int port)
        {
            PrepareAndConnect(serverAddress, port);
        }

        /// <inheritdoc/>
        public ServerInfo GetServerInfo()
        {
            const string requestPayload = "Source Engine Query\0";
            var sInfo = new ServerInfo
            {
                Ping = new Ping().Send(_ipEndpoint.Address).RoundtripTime
            };

            byte[] response = SendRequest(RequestHeaders.A2S_INFO, Encoding.UTF8.GetBytes(requestPayload));
            if (response.Length > 0)
            {
                ExtractData(sInfo, response, nameof(sInfo.EDF), true);
            }

            return sInfo;
        }

        /// <inheritdoc/>
        public void RenewChallenge()
        {
            byte[] response = SendRequest(RequestHeaders.A2S_PLAYER, BitConverter.GetBytes(-1));
            if (response.Length > 0)
            {
                _currentChallenge = BitConverter.ToInt32(response.Skip(RESPONSE_CODE_INDEX).Take(sizeof(int)).ToArray(), 0);
            }
        }

        /// <inheritdoc/>
        public List<Player> GetPlayers()
        {
            if (_currentChallenge == 0)
            {
                RenewChallenge();
            }

            byte[] response = SendRequest(RequestHeaders.A2S_PLAYER, BitConverter.GetBytes(_currentChallenge));
            if (response.Length > 0)
            {
                return ExtractListData<Player>(response);
            }
            else
            {
                throw new InvalidOperationException("Server did not response the query");
            }
        }

        /// <inheritdoc/>
        public List<Rule> GetRules()
        {
            if (_currentChallenge == 0)
            {
                RenewChallenge();
            }

            byte[] response = SendRequest(RequestHeaders.A2S_RULES, BitConverter.GetBytes(_currentChallenge));
            if (response.Length > 0)
            {
                var rls = ExtractListData<Rule>(response);
                return ExtractListData<Rule>(response);
            }
            else
            {
                throw new InvalidOperationException("Server did not response the query");
            }
        }

        /// <summary>
        /// Disposes the object and its disposables.
        /// </summary>
        public void Dispose()
        {
            _client.Close();
            _client.Dispose();
        }

        private void PrepareAndConnect(string serverAddress, int port)
        {
            _port = port;

            // Check the port range
            if (_port < IPEndPoint.MinPort || _port > IPEndPoint.MaxPort)
            {
                throw new ArgumentException($"Port should be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}");
            }

            // Try to parse the serverAddress as IP first
            if (IPAddress.TryParse(serverAddress, out IPAddress parsedIpAddress))
            {
                // Yep its an IP.
                _ipEndpoint = new IPEndPoint(parsedIpAddress, _port);
            }
            else
            {
                // Nope it might be a hostname.
                try
                {
                    IPAddress[] addresslist = Dns.GetHostAddresses(serverAddress);
                    if (addresslist.Length > 0)
                    {
                        // We get the first address.
                        _ipEndpoint = new IPEndPoint(addresslist[0], _port);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid host address {serverAddress}");
                    }
                }
                catch (SocketException ex)
                {
                    throw new ArgumentException("Could not reach the hostname.", ex);
                }
            }

            _client.Client.SendTimeout = SendTimeout;
            _client.Client.ReceiveTimeout = ReceiveTimeout;
            _client.Connect(_ipEndpoint);
        }

        private List<TObject> ExtractListData<TObject>(byte[] rawSource)
            where TObject : class
        {
            // Create a list to contain the serialized data.
            var objectList = new List<TObject>();

            // Skip the response headers.
            IEnumerable<byte> dataSource = rawSource.Skip(RESPONSE_HEADER_COUNT);

            // Iterate amount of times that the server said.
            for (byte i = 0; i < rawSource[RESPONSE_CODE_INDEX]; i++)
            {
                // Activate a new instance of the object.
                var objectInstance = Activator.CreateInstance<TObject>();

                // Extract the data.
                dataSource = ExtractData(objectInstance, dataSource.ToArray());

                // Add it into the list.
                objectList.Add(objectInstance);
            }

            return objectList;
        }

        private byte[] SendRequest(byte requestHeader, byte[] payload = null)
        {
            var request = BuildRequest(requestHeader, payload);
            _client.Send(request, request.Length);
            return _client.Receive(ref _ipEndpoint);
        }

        private byte[] BuildRequest(byte headerCode, byte[] extraParams = null)
        {
            /* All requests consists 4 FF's and a header code to execute the request.
             * Check here: https://developer.valvesoftware.com/wiki/Server_queries#Protocol for further information about the protocol. */
            var request = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, headerCode };

            // If we have any extra payload, concatenate those into our requestHeaders and return;
            return extraParams != null ? request.Concat(extraParams).ToArray() : request;
        }

        private IEnumerable<byte> ExtractData<TObject>(TObject objectRef, byte[] dataSource, string edfPropName = "", bool stripHeaders = false)
            where TObject : class
        {
            IEnumerable<byte> takenBytes = new List<byte>();

            // We can be a good guy and ask for any extra jobs :)
            IEnumerable<byte> enumerableSource = stripHeaders ? dataSource.Skip(RESPONSE_HEADER_COUNT) : dataSource;

            // We get every property that does not contain ParseCustom and NotParsable attributes on them to iterate through all and parse/assign their values.
            IEnumerable<PropertyInfo> propsOfObject = typeof(TObject).GetProperties()
                .Where(x => x.CustomAttributes.Count(y => y.AttributeType == typeof(ParseCustomAttribute)
                                                       || y.AttributeType == typeof(NotParsableAttribute)) == 0);

            foreach (PropertyInfo property in propsOfObject)
            {
                /* Check for EDF property name, if it was provided then it mean that we have EDF properties in the model.
                 * You can check here: https://developer.valvesoftware.com/wiki/Server_queries#A2S_INFO to get more info about EDF's. */
                if (!string.IsNullOrEmpty(edfPropName))
                {
                    // Does the property have an EDFAttribute assigned ?
                    CustomAttributeData edfInfo = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(EDFAttribute));
                    if (edfInfo != null)
                    {
                        // Get the EDF value that was returned by the server.
                        byte edfValue = (byte)typeof(TObject).GetProperty(edfPropName).GetValue(objectRef);

                        // Get the EDF condition value that was provided in the model.
                        byte edfPropertyConditionValue = (byte)edfInfo.ConstructorArguments[0].Value;

                        // Continue if the condition does not pass because it means that the server did not include any information about this property.
                        if ((edfValue & edfPropertyConditionValue) <= 0) { continue; }
                    }
                }

                /* Basic explanation of what is going of from here;
                 * Get the type of the property and get amount of bytes of its size from the response array,
                 * Convert the parsed value to its type and assign it.
                 */

                /* We have to handle strings separately since their size is unknown and they are also null terminated.
                 * Check here: https://developer.valvesoftware.com/wiki/String for further information about Strings in the protocol.
                 */
                if (property.PropertyType == typeof(string))
                {
                    // Take till the termination.
                    takenBytes = enumerableSource.TakeWhile(x => x != 0);

                    // Parse it into a string.
                    property.SetValue(objectRef, Encoding.UTF8.GetString(takenBytes.ToArray()));

                    // Update the source by skipping the amount of bytes taken from the source and + 1 for termination byte.
                    enumerableSource = enumerableSource.Skip(takenBytes.Count() + 1);
                }
                else
                {
                    // Is the property an Enum ? if yes we should be getting the underlying type since it might differ.
                    Type typeOfProperty = property.PropertyType.IsEnum ? property.PropertyType.GetEnumUnderlyingType() : property.PropertyType;

                    // Extract the value and the size from the source.
                    (object result, int size) = ExtractMarshalType(enumerableSource, typeOfProperty);

                    /* If the property is an enum we should parse it first then assign its value,
                     * if not we can just give it to SetValue since it was converted by ExtractMarshalType already.*/
                    property.SetValue(objectRef, property.PropertyType.IsEnum ? Enum.Parse(property.PropertyType, result.ToString()) : result);

                    // Update the source by skipping the amount of bytes taken from the source.
                    enumerableSource = enumerableSource.Skip(size);
                }
            }

            // We return the last state of the processed source.
            return enumerableSource;
        }

        private (object, int) ExtractMarshalType(IEnumerable<byte> source, Type type)
        {
            // Get the size of the given type.
            int sizeOfType = Marshal.SizeOf(type);

            // Take amount of bytes from the source array.
            IEnumerable<byte> takenBytes = source.Take(sizeOfType);

            // We actually need to go into an unsafe block here since as far as i know, this is the only way to convert a byte[] source into its given type on runtime.
            unsafe
            {
                fixed (byte* sourcePtr = takenBytes.ToArray())
                {
                    return (Marshal.PtrToStructure(new IntPtr(sourcePtr), type), sizeOfType);
                }
            }
        }
    }
}
