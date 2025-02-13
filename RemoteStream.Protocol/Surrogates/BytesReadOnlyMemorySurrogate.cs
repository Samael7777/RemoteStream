using ProtoBuf;

namespace RemoteStream.Protocol.Surrogates;

[Surrogate(typeof(ReadOnlyMemory<byte>))]
[ProtoContract(SkipConstructor = true)]
public class BytesReadOnlyMemorySurrogate(byte[] value)
{
    [ProtoMember(1, IsRequired = true)] private readonly byte[] _bytes = value;

    public static implicit operator ReadOnlyMemory<byte>(BytesReadOnlyMemorySurrogate surrogate)
    {
        return new ReadOnlyMemory<byte>(surrogate._bytes);
    }

    public static implicit operator BytesReadOnlyMemorySurrogate(ReadOnlyMemory<byte> value)
    {
        return new BytesReadOnlyMemorySurrogate(value.ToArray());
    }
}