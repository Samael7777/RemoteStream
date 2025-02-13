namespace RemoteStream.Server.Factory;

[AttributeUsage(AttributeTargets.Class)]
public class StreamImplementationAttribute(Type target, Type grpcInterface) : Attribute
{
    public Type Target { get; } = target;
    public Type GrpcInterface { get; } = grpcInterface;
}