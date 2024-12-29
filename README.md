# RemoteStream

Network-distributed stream with gRpc connection .

Client stream supports `async` operations.

Use [PhoenixTools.RemoteStream.Client ](https://www.nuget.org/packages/PhoenixTools.RemoteStream.Client/ "`PhoenixTools.RemoteStream.Client` ")nuget package for 
a client-part stream, and [PhoenixTools.RemoteStream.Server](http://https://www.nuget.org/packages/PhoenixTools.RemoteStream.Server/ "PhoenixTools.RemoteStream.Server") for a server-part.

Usage sample (server-side):

```javascript
//Server endpoint
var ep = new IPEndPoint(IPAddress.Parse("10.10.10.10"), 7777);

//SSL certificate for HTTPS connection
var cert = File.ReadAllText("server.crt");
var key = File.ReadAllText("server.key");

//Source stream
var srcStream = File.Open("sample.bin", FileMode.Open);
var serverStream = new RemoteStreamServer(srcStream, ep, cert, key);
serverStream.ExceptionThrew += (_, exception) =>
{
    //Exception handling...
};

serverStream.StreamClosed += (_, _) =>
{
    //Stream close event handling...
};

//Other code....

//Dispose server
serverStream.Dispose();

//Dispose base stream
srcStream.Dispose();
```

Client-side sample

```javascript
//Server endpoint
var ep = new IPEndPoint(IPAddress.Parse("10.10.10.10"), 7777);

//Create client-part of the stream
var clientStream = new RemoteStreamClient(ep, ConnectionType.Https);

//Create destination stream
var localFile = File.OpenWrite("dst.bin");

//Copy remote file to local
clientStream.CopyTo(localFile);

//Dispose streams
clientStream.Dispose();
localFile.Dispose();
```
