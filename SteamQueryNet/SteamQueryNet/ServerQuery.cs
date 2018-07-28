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
                var request = BuildRequest(RequestHeaders.A2S_INFO, Encoding.UTF8.GetBytes(requestPayload));
                _client.Send(request, request.Length);
                byte[] response = _client.Receive(ref _ipEndpoint);
                if (response.Length > 0)
                {
                    IEnumerable<byte> lastSource = ExtractData(sInfo, response, nameof(sInfo.EDF));

                    // Handle EDF's. This part looks hideous but i will get back to this after i get done with everything. Right now this works.
                    if ((sInfo.EDF & 0x80) > 0)
                    {
                        (object result, int size) = ExtractMarshalType(lastSource, sInfo.Port.GetType());
                        sInfo.Port = (short)result;
                        lastSource = lastSource.Skip(size);
                    }
                    if ((sInfo.EDF & 0x10) > 0)
                    {
                        (object result, int size) = ExtractMarshalType(lastSource, sInfo.SteamID.GetType());
                        sInfo.SteamID = (long)result;
                        lastSource = lastSource.Skip(size);
                    }
                    if ((sInfo.EDF & 0x40) > 0)
                    {
                        (object result, int size) = ExtractMarshalType(lastSource, sInfo.SourceTVPort.GetType());
                        sInfo.SourceTVPort = (short)result;
                        lastSource = lastSource.Skip(size);

                        IEnumerable<byte> takenBytes = lastSource.TakeWhile(x => x != 0);
                        sInfo.SourceTVServerName = Encoding.UTF8.GetString(takenBytes.ToArray());
                        lastSource = lastSource.Skip(takenBytes.Count() + 1);

                    }
                    if ((sInfo.EDF & 0x20) > 0)
                    {
                        IEnumerable<byte> takenBytes = lastSource.TakeWhile(x => x != 0);
                        sInfo.Keywords = Encoding.UTF8.GetString(takenBytes.ToArray());
                        lastSource = lastSource.Skip(takenBytes.Count() + 1);
                    }
                    if ((sInfo.EDF & 0x01) > 0)
                    {
                        (object result, int size) = ExtractMarshalType(lastSource, sInfo.GameID.GetType());
                        sInfo.GameID = (long)result;
                        lastSource = lastSource.Skip(size);
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Log it.
                throw;
            }

            return sInfo;
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

        private IEnumerable<byte> ExtractData<TObject>(TObject objectRef, byte[] dataSource, string stopAt = "")
            where TObject : class
        {
            IEnumerable<byte> takenBytes = new List<byte>();
            IEnumerable<byte> strippedSource = dataSource.Skip(5);
            IEnumerable<PropertyInfo> propsOfObject = typeof(TObject).GetProperties()
                .Where(x => x.CustomAttributes.Count(y => y.AttributeType == typeof(ParseCustomAttribute)
                                                       || y.AttributeType == typeof(NotParsableAttribute)) == 0);

            foreach (PropertyInfo property in propsOfObject)
            {
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

                if (property.Name == stopAt)
                {
                    break;
                }
            }

            return strippedSource;
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
