using System.IO;
using System.Reflection;

namespace SharedCode;

public static class EmbeddedFileHelper
{
    public static string? ReadEmbeddedTextFile(string fileName)
    {
        var assembly = Assembly.GetCallingAssembly();
        using var stream = assembly.GetManifestResourceStream(fileName);
        if(stream is null) return null;
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}