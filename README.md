# SteamQueryNet

SteamQueryNet is a C# wrapper for [Steam Server Queries](https://developer.valvesoftware.com/wiki/Server_queries) UDP protocol. It is;

* Light
* Dependency free
* Written in .NET 6

# How to install ?

Check out [SteamQueryNet](https://www.nuget.org/packages/SteamQueryNet/) on nuget.

# How to use ?

SteamQueryNet comes with a single object that gives you access to all API's of the [Steam protocol](https://developer.valvesoftware.com/wiki/Server_queries) which are;

* Server information (server name, capacity etc).
* Concurrent players.
* Server rules (friendlyfire, roundttime etc). **Warning: currently does not work due to a protocol issue on steam server query API. Use could make use of ServerInfo.tags if the server admins are kind enough to put rules as tags in the field.**

## Creating an instance
To make use of the API's listed above, an instance of `ServerQuery` should be created.

```csharp
IServerQuery serverQuery = new ServerQuery();
serverQuery.Connect(host, port);
```

or you can use string resolvers like below:

```csharp
    string myHostAndPort = "127.0.0.1:27015";
    // or
    myHostAndPort = "127.0.0.1,27015";
    // or
    myHostAndPort = "localhost:27015";
    // or
    myHostAndPort = "localhost,27015";
    // or
    myHostAndPort = "steam://connect/127.0.0.1:27015";
    // or
    myHostAndPort = "steam://connect/localhost:27015";

    IServerQuery serverQuery = new ServerQuery(myHostAndPort);
```

## Providing Custom UDPClient

You can provide custom UDP clients by implementing `IUdpClient` in `SteamQueryNet.Interfaces` namespace.

See the example below:
```csharp
public class MyAmazingUdpClient : IUdpClient
    {
        public bool IsConnected { get; }

        public void Close()
        {
            // client implementation
        }

        public void Connect(IPEndPoint remoteIpEndpoint)
        {
            // client implementation
        }

        public void Dispose()
        {
            // client implementation
        }

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            // client implementation
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            // client implementation
        }
    }

    // Usage
    IPEndpoint remoteIpEndpoint = new IPEndPoint(IPAddress.Parse(remoteServerIp), remoteServerPort);

    IUdpClient myUdpClient = new MyAmazingUdpClient();
    IServerQuery serverQuery = new ServerQuery(myUdpClient, remoteIpEndpoint);
```

once its created functions below returns informations desired,

[ServerInfo](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/ServerInfo.cs)
```csharp
ServerInfo serverInfo = serverQuery.GetServerInfo();
```

[Players](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/Player.cs)
```csharp
List<Player> players = serverQuery.GetPlayers();
```

[Rules](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/Rule.cs)
```csharp
List<Rule> rules = serverQuery.GetRules();
```

# Todos
* Enable CI
