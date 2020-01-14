# Changelog for SteamQueryNet v1.0.6

### 1. Enhancements

* ServerInfo model now reduces the `ServerInfo.Bots` from the `ServerInfo.Players` property. So that `ServerInfo.Players` reflects real player count only. This was an issue because some server flag their bots as real players.

* Added a new constructor to `ServerQuery` to allow users to be able to bind their own local IPEndpoint.

* Added a new constructor to `ServerQuery` to allow users to be able to provide hostnames and ports in one single string like
    
    ```
    string myHostAndPort = "127.0.0.1:27015";
    // or
    string myHostAndPort = "localhost:27015";
    // or
    string myHostAndPort = "steam://connect/127.0.0.1:27015";
    // or
    string myHostAndPort = "steam://connect/localhost:27015";
    ```
* Implemented new tests for ip, hostname and port validation.

* Added a CHANGELOG. LUL :)

### 2. Bug fixes

* Fixed a bug where player information was not gathered correctly by the `ServerQuery.GetPlayers()` function.

* Fixed a bug where player count was not gathered by the `ServerQuery.GetServerInfo()` function.

### 3. Soft-deprecations (no warnings emitted)

* Removed `sealed` modifiers from all `SteamQueryNet.Models` namespace.

* `IServerQuery` moved into `SteamQueryNet.Interfaces` namespace.

### 4. Hard-deprecations

* `ServerQuery` constructor parameter `int port` now changed to `ushort` to remove all integer range checks since the UDP port is already literally an `ushort`.

* Removed port range tests.

* `IServerQuery` moved into `SteamQueryNet.Interfaces` namespace.