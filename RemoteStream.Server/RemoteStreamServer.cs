using System.Net;
using Grpc.Core;
using RemoteStream.Protocol;

namespace RemoteStream.Server;

public class RemoteStreamServer : IDisposable
{
    private readonly Grpc.Core.Server _server;
    private readonly RemoteStreamServerRpcImpl _rpcService;
    public RemoteStreamServer(Stream stream, IPEndPoint localEndPoint)
    {
        _rpcService = new RemoteStreamServerRpcImpl(stream);
        _rpcService.StreamClosed += (_, _) => Dispose();

        _server = new Grpc.Core.Server()
        {
            //todo: add secure
            Ports = { new ServerPort(localEndPoint.Address.ToString(), localEndPoint.Port, ServerCredentials.Insecure) },
            Services = { RemoteStreamRpcService.BindService(_rpcService) }
        };
        _server.Start();
    }

    #region Dispose

    private bool _disposed;

    ~RemoteStreamServer()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            //dispose managed state (managed objects)
            _server.KillAsync().Wait();
            _rpcService.Dispose();
        }
        //free unmanaged resources (unmanaged objects) and override finalizer
        //set large fields to null

        _disposed = true;
    }

    #endregion
}