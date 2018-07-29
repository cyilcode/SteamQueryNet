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
        void RenewChallenge();
        ServerInfo GetServerInfo();
    }

    public class ServerQuery : IServerQuery, IDisposable
    {
        private readonly int _port;
        private readonly UdpClient _client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        private IPEndPoint _ipEndpoint;

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
        /// Creates a new ServerQuery instance for Steam Server Query Operations.
        /// </summary>
        /// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
        /// <param name="port">Port of the server that queries will be sent.</param>
        public ServerQuery(string serverAddress, int port)
        {
            // Check the port range
            if (_port < IPEndPoint.MinPort || _port > IPEndPoint.MaxPort)
            {
                throw new ArgumentException($"Port should be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}");
            }

            _port = port;
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

        public void Dispose()
        {
            _client.Close();
            _client.Dispose();
        }

        public ServerInfo GetServerInfo()
        {
            var sInfo = new ServerInfo();
            const string requestPayload = "Source Engine Query\0";
            try
            {
                sInfo.Ping = new Ping().Send(_ipEndpoint.Address).RoundtripTime;
                byte[] response = SendRequest(RequestHeaders.A2S_INFO, Encoding.UTF8.GetBytes(requestPayload));
                if (response.Length > 0)
                {
                    ExtractData(sInfo, response, nameof(sInfo.EDF));
                }
            }
            catch (Exception)
            {
                // TODO: Log it.
                throw;
            }

            return sInfo;
        }

        private byte[] SendRequest(byte requestHeader, byte[] payload = null)
        {
            var request = BuildRequest(requestHeader, payload);
            _client.Send(request, request.Length);
            return _client.Receive(ref _ipEndpoint);
        }

        public void RenewChallenge()
        {
            throw new NotImplementedException();
        }

        private byte[] BuildRequest(byte headerCode, byte[] extraParams = null)
        {
            var request = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, headerCode };
            return extraParams != null ? request.Concat(extraParams).ToArray() : request;
        }

        private void ExtractData<TObject>(TObject objectRef, byte[] dataSource, string edfPropName = "")
            where TObject : class
        {
            IEnumerable<byte> takenBytes = new List<byte>();
            IEnumerable<byte> strippedSource = dataSource.Skip(5);
            IEnumerable<PropertyInfo> propsOfObject = typeof(TObject).GetProperties()
                .Where(x => x.CustomAttributes.Count(y => y.AttributeType == typeof(ParseCustomAttribute)
                                                       || y.AttributeType == typeof(NotParsableAttribute)) == 0);

            foreach (PropertyInfo property in propsOfObject)
            {
                CustomAttributeData edfInfo = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(EDFAttribute));
                if (edfInfo != null)
                {
                    byte edfValue = (byte)typeof(TObject).GetProperty(edfPropName).GetValue(objectRef);
                    byte edfPropertyConditionValue = (byte)edfInfo.ConstructorArguments[0].Value;
                    if ((edfValue & edfPropertyConditionValue) <= 0) { continue; }
                }

                if (property.PropertyType == typeof(string))
                {
                    takenBytes = strippedSource.TakeWhile(x => x != 0);
                    property.SetValue(objectRef, Encoding.UTF8.GetString(takenBytes.ToArray()));
                    strippedSource = strippedSource.Skip(takenBytes.Count() + 1); // +1 for null termination
                }
                else
                {
                    Type typeOfProperty = property.PropertyType.IsEnum ? property.PropertyType.GetEnumUnderlyingType() : property.PropertyType;
                    (object result, int size) = ExtractMarshalType(strippedSource, typeOfProperty);
                    property.SetValue(objectRef, property.PropertyType.IsEnum ? Enum.Parse(property.PropertyType, result.ToString()) : result);
                    strippedSource = strippedSource.Skip(size);
                }
            }
        }

        private (object, int) ExtractMarshalType(IEnumerable<byte> source, Type type)
        {
            int sizeOfType = Marshal.SizeOf(type);
            IEnumerable<byte> takenBytes = source.Take(sizeOfType);
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
