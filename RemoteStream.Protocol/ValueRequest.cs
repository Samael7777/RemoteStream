using ProtoBuf;

namespace RemoteStream.Protocol;

[ProtoContract(SkipConstructor = true)]
public class ValueRequest<T>
where T : struct
{
    [ProtoMember(1)]
    public T Value { get; set; }

    public static  ValueRequest<T> FromValue(T value)
    {
        return new ValueRequest<T> { Value = value };
    }
}