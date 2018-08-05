# SteamQueryNet

SteamQueryNet is a C# wrapper for [Steam Server Queries](https://developer.valvesoftware.com/wiki/Server_queries) UDP protocol. It is;

* Light
* Dependency free
* Written in .net standard 2.0 so that it works with both .NET framework 4.6+ and core.

# How to install ?

Check out [SteamQueryNet](https://www.nuget.org/packages/SteamQueryNet/) on nuget.

# How to use ?

SteamQueryNet comes with a single object that gives you access to all API's of the [Steam protocol](https://developer.valvesoftware.com/wiki/Server_queries) which are;

* Server information (server name, capacity etc).
* Concurrent players.
* Server rules (friendlyfire, roundttime etc). **Warning: currently does not work due to a protocol issue on server source server.**

## Creating an instance
To make us of the API's listed above, an instance of `ServerQuery` should be created.

```csharp
string serverIp = "127.0.0.1";
int serverPort = 27015;
IServerQuery serverQuery = new ServerQuery(serverIp, serverPort);
```

once its created functions below returns informations desired,

[ServerInfo](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/ServerInfo.cs)
```csharp
ServerInfo serverInfo = serverQuery.GetServerInfo();
```

[Players](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/Player.cs)
```csharp
// Concurrent Players
List<Player> players = serverQuery.GetPlayers();
```

[Rules](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/Rule.cs)
```csharp
// Rules
List<Rules> rules = serverQuery.GetRules();
```

and thats it.

# Todos
* Write MOAR TESTS !
* Enable CI
