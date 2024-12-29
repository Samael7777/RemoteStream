using System.Reflection;

namespace RemoteStream.Tests;

public static class AssemblyEx
{
    public static string GetTextFromResource(this Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}