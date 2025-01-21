using System.Diagnostics;
using System.Reflection;
using ProtoBuf;

namespace RemoteStream.Protocol.Exceptions;

[DebuggerDisplay("{Type}")]
[ProtoContract(SkipConstructor = true)]
public class ExceptionSurrogate
{
    [ProtoMember(1, IsRequired = true)]
    public Type Type { get; set; } = typeof(Exception);

    [ProtoMember(2)]
    public string? Message { get; set; }

    [ProtoMember(3)]
    public int HResult { get; set; }

    [ProtoMember(4)]
    public ExceptionSurrogate? InnerException { get; set; }

    [ProtoMember(5)]
    public string? HelpLink { get; set; }

    [ProtoMember(6)]
    public string? StackTrace { get; set; }

    [ProtoMember(7)]
    public string? Source { get; set; }

    [ProtoMember(8)]
    public string? TargetSiteString { get; set; }
    

    public static implicit operator Exception? (ExceptionSurrogate? surrogate)
    {
        if (surrogate == null) return null;

        var exception = DeserializeException(surrogate);
        return exception;
    }

    public static implicit operator ExceptionSurrogate?(Exception? ex)
    {
        if (ex == null) return null;

        return new ExceptionSurrogate
        {
            Type = ex.GetType(),
            Message = ex.Message,
            HResult = ex.HResult,
            InnerException = ex.InnerException,
            HelpLink = ex.HelpLink,
            StackTrace = ex.StackTrace,
            Source = ex.Source,
            TargetSiteString = ex.TargetSite != null ? $"{ex.TargetSite.DeclaringType} {ex.TargetSite}" : null
        };
    }
    
    private static Exception? DeserializeException(ExceptionSurrogate? surrogate)
    {
        if (surrogate == null) return null;

        var innerException = DeserializeException(surrogate.InnerException);
        var exception = (Exception?)Activator.CreateInstance(surrogate.Type);
        
        if (exception == null)
            throw new InvalidCastException();
        
        exception.HResult = surrogate.HResult;
        exception.HelpLink = surrogate.HelpLink;
        exception.Source = surrogate.Source;

        SetPrivateFieldValue(exception, "_stackTraceString", surrogate.StackTrace);
        SetPrivateFieldValue(exception, "_message", surrogate.Message);
        SetPrivateFieldValue(exception, "_innerException", innerException);
        
        exception.Data.Add("TargetSite", surrogate.TargetSiteString);
        
        return exception;
    }
    
    private static void SetPrivateFieldValue(Exception instance, string name, object? value)
    {
        var type = typeof(Exception);
        var fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        fieldInfo?.SetValue(instance, value);
    }
}