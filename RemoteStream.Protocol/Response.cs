using ProtoBuf;
using ProtoBuf.Meta;
using RemoteStream.Protocol.Exceptions;

namespace RemoteStream.Protocol;

[ProtoContract(SkipConstructor = true)]
public class Response<T>
{
    static Response()
    {
        var model = RuntimeTypeModel.Default;
        model.Add(typeof(Exception), false)
            .SetSurrogate(typeof(ExceptionSurrogate));
    }
    
    private Response(T? value)
    {
        _value = value;
    }

    private Response(Exception ex)
    {
        _exception = ex;
    }

    [ProtoMember(1)] private readonly T? _value;
    [ProtoMember(2)] private readonly Exception? _exception;

    public bool IsSuccess => _exception == null;

    public T? GetValueOrException()
    {
        if (_exception != null)
            throw _exception;

        return _value;
    }

    public static Response<T> FromValue(T? value)
    {
        return new Response<T>(value);
    }

    public static Response<T> FromException(Exception ex)
    {
        return new Response<T>(ex);
    }
}