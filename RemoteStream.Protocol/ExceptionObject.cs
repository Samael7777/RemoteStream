using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RemoteStream.Protocol;

[DebuggerDisplay("{Message}")]
public class ExceptionObject
{
    public string Type { get; }
    public string? Message { get; }
    public int HResult { get; }
    public ExceptionObject? InnerException { get; }
    public string? HelpLink { get; }
    public string? StackTrace { get; }
    public string? Source { get; }
    public IDictionary? Data { get; }
    public string? TargetSite { get; }

    public ExceptionObject(Exception ex)
    {
        Type = ex.GetType().ToString();
        Message = ex.Message;
        HResult = ex.HResult;
        InnerException = ex.InnerException != null ? new ExceptionObject(ex.InnerException) : null;
        HelpLink = ex.HelpLink;
        StackTrace = ex.StackTrace;
        Source = ex.Source;
        Data = ex.Data;
        TargetSite = ex.TargetSite?.ToString();
    }

    [JsonConstructor]
    internal ExceptionObject(string type, string? message, int hResult, ExceptionObject? innerException, string? helpLink, string? stackTrace, string? source, IDictionary data, string? targetSite)
    {
        Type = type;
        Message = message;
        HResult = hResult;
        InnerException = innerException;
        HelpLink = helpLink;
        StackTrace = stackTrace;
        Source = source;
        Data = data;
        TargetSite = targetSite;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, ExceptionObjectSerializerContext.Default.ExceptionObject);
    }

    public static ExceptionObject FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, ExceptionObjectSerializerContext.Default.ExceptionObject)
               ?? throw new ArgumentException("Incorrect json string.", nameof(json));
    }
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(ExceptionObject))]
internal partial class ExceptionObjectSerializerContext : JsonSerializerContext;