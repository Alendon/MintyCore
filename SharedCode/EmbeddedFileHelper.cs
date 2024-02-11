// ReSharper disable once RedundantUsingDirective
using System.IO;
using System.Reflection;
using JetBrains.Annotations;

namespace SharedCode;

[PublicAPI]
internal static class EmbeddedFileHelper
{
    public static string? ReadEmbeddedTextFile(string fileName)
    {
        var assembly = Assembly.GetCallingAssembly();
        using var stream = assembly.GetManifestResourceStream(fileName);
        if (stream is null)
        {
            return null;
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    public static string ReadEmbeddedTextFileOrThrow(string fileName)
    {
        var result = ReadEmbeddedTextFile(fileName);
        if (result is null)
        {
            throw new FileNotFoundException($"Embedded file '{fileName}' not found.");
        }
        
        return result;
    }
}