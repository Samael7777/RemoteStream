using Grpc.Core;
using ProtoBuf.Grpc.Server;
using RemoteStream.Protocol.Interfaces;
using RemoteStream.Server.Factory;
using RemoteStream.Server.Implementations;

namespace RemoteStream.Server;

public class RemoteStreamServer<T> : IDisposable, IAsyncDisposable
    where T: Stream
{
    private readonly Grpc.Core.Server _server;
    private readonly RemoteStreamImpl _implementation;

    public event EventHandler<RemoteStreamExceptionArgs>? ExceptionThrew;
    public event EventHandler? StreamClosed;
    
    public RemoteStreamServer(T baseStream, string host, int port, string? cert, string? key, bool disposeStream = true)
    {
        var credentials = BuildSslCredentials(cert, key);

        _server = new Grpc.Core.Server
        {
            Ports = { new ServerPort(host, port, credentials) }
        };

        _implementation = RemoteStreamImplFactory.GetRemoteStreamImpl(baseStream, disposeStream);
        _implementation.ExceptionThrew += (_, a) => ExceptionThrew?.Invoke(this, a);
        _implementation.StreamClosed += (_, a) => StreamClosed?.Invoke(this, a);

        _server.Services.AddCodeFirst<IRemoteStream>(_implementation);
        _server.Start();
    }

    public RemoteStreamServer(T baseStream, string host, int port, bool disposeStream = true) 
        : this(baseStream, host, port, null, null, disposeStream)
    { }

    private static ServerCredentials BuildSslCredentials(string? cert, string? key)
    {
        if (string.IsNullOrWhiteSpace(cert) || string.IsNullOrWhiteSpace(key))
            return ServerCredentials.Insecure;

        var keyCertPair = new KeyCertificatePair(cert, key);

        return new SslServerCredentials([keyCertPair]);
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeAsyncCore().AsTask().Wait();
            StreamClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _server.ShutdownAsync().ConfigureAwait(false);
        await _implementation.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    #endregion
}