namespace RemoteStream.Protocol.Surrogates;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class SurrogateAttribute(Type targetType) : Attribute
{
    public Type TargetType { get; } = targetType;
}