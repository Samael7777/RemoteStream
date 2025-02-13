using System.Runtime.Versioning;
using ProtoBuf.Grpc;
using RemoteStream.Protocol;
using RemoteStream.Protocol.Interfaces;
using RemoteStream.Server.Factory;

// ReSharper disable ConvertToPrimaryConstructor

namespace RemoteStream.Server.Implementations;

[StreamImplementation(typeof(FileStream), typeof(IRemoteFileStream))]
public class RemoteFileStreamImpl : RemoteStreamImpl, IRemoteFileStream
{
    public RemoteFileStreamImpl(FileStream stream, bool disposeStream) 
        : base(stream, disposeStream)
    { }

    public Task<Response<FileStreamInfo>> GetFileStreamInfoAsync()
    {
        var info = new FileStreamInfo
        {
            IsAsync = ((FileStream)_baseStream).IsAsync,
            Name = ((FileStream)_baseStream).Name
        };

        return Response<FileStreamInfo>.TaskFromValue(info);
    }

    public Task<Response> FlushAsync(ValueRequest<bool> flushToDisk, CallContext context = default)
    {
        try
        {
            ((FileStream)_baseStream).Flush(flushToDisk.Value);
            
            return Response.TaskFromSuccess();
        }
        catch (Exception e)
        {
            OnExceptionThrew(e);
           
            return Response.TaskFromException(e);
        }
    }

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("freebsd")]
    public Task<Response> Lock(ValueRequest<long> position, ValueRequest<long> length)
    {
        try
        {
            ((FileStream)_baseStream).Lock(position.Value, length.Value);
            
            return Response.TaskFromSuccess();
        }
        catch (Exception e)
        {
            OnExceptionThrew(e);
           
            return Response.TaskFromException(e);
        }
    }

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("freebsd")]
    public Task<Response> Unlock(ValueRequest<long> position, ValueRequest<long> length)
    {
        try
        {
            ((FileStream)_baseStream).Unlock(position.Value, length.Value);
            
            return Response.TaskFromSuccess();
        }
        catch (Exception e)
        {
            OnExceptionThrew(e);
           
            return Response.TaskFromException(e);
        }
    }
}