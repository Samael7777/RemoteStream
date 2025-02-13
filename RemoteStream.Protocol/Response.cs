using ProtoBuf;

namespace RemoteStream.Protocol;

[ProtoContract(SkipConstructor = true)]
[ProtoInclude(100, typeof(Response<StreamInfo>))]
[ProtoInclude(101, typeof(Response<long>))]
[ProtoInclude(102, typeof(Response<ReadOnlyMemory<byte>>))]
[ProtoInclude(103, typeof(Response<FileStreamInfo>))]
public class Response
{
    [field: ProtoMember(1)]
    public Exception? Exception { get; }

    [field: ProtoMember(2)] 
    public bool IsSuccess { get; }

    protected Response()
    {
        IsSuccess = true;
    }

    protected Response(Exception ex)
    {
        Exception = ex;
        IsSuccess = false;
    }

    public void ThrowExceptionWhenError()
    {
        if (IsSuccess) return;

        throw Exception ?? new ApplicationException("Unknown error.");
    }

    public static Response FromSuccess() 
        => new();
    
    public static Task<Response> TaskFromSuccess() 
        => Task.FromResult(FromSuccess());

    public static Response FromException(Exception exception) 
        => new(exception);

    public static Task<Response> TaskFromException(Exception exception) 
        => Task.FromResult(FromException(exception));
}

[ProtoContract(SkipConstructor = true)]
#pragma warning disable PBN0013
public sealed class Response<T> : Response
#pragma warning restore PBN0013
{
    [ProtoMember(1)] private readonly T? _value;

    public T Value
    {
        get
        {
            if (IsSuccess && _value != null)
                return _value;

            throw Exception ?? new ApplicationException("Unknown error.");
        }
    }

    private Response(Exception ex) : base(ex) {}

    private Response(T value) 
    {
        _value = value;
    }
    
    public static Response<T> FromValue(T value) 
        => new (value);

    public static Task<Response<T>> TaskFromValue(T value) 
        => Task.FromResult(FromValue(value));

    public new static Response<T> FromException(Exception ex) 
        => new(ex);

    public new static Task<Response<T>>TaskFromException(Exception ex)
        => Task.FromResult(FromException(ex));
}