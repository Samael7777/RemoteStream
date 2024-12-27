using System.Net;
using Grpc.Core;
using RemoteStream.Protocol;

namespace RemoteStream.Server;

public class RemoteStreamServer : IDisposable
{
    private readonly Grpc.Core.Server _server;
    private readonly RemoteStreamServerRpcImpl _rpcService;

    public event EventHandler<Exception>? ExceptionThrew;
    public event EventHandler? StreamClosed;
    
    public RemoteStreamServer(Stream stream, IPEndPoint localEndPoint)
        :this(stream, localEndPoint, ServerCredentials.Insecure)
    { }

    public RemoteStreamServer(Stream stream, IPEndPoint localEndPoint, string cert, string key)
        :this(stream, localEndPoint, BuildSslCredentials(cert, key))
    { }

    public RemoteStreamServer(Stream stream, IPEndPoint localEndPoint, ServerCredentials credentials)
    {
        _rpcService = new RemoteStreamServerRpcImpl(stream);
        _rpcService.ExceptionThrew += (_, ex) => ExceptionThrew?.Invoke(this, ex);
        _rpcService.StreamClosed += (_, _) => StreamClosed?.Invoke(this, EventArgs.Empty);

        _server = new Grpc.Core.Server()
        {
            Ports = { new ServerPort(localEndPoint.Address.ToString(), localEndPoint.Port, credentials) },
            Services = { RemoteStreamRpcService.BindService(_rpcService) }
        };
        _server.Start();
    }

    private static SslServerCredentials BuildSslCredentials(string cert, string key)
    {
        var keyCertPair = new KeyCertificatePair(cert, key);
        return new SslServerCredentials([keyCertPair]);
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