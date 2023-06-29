using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Veldrid;
using Veldrid.SPIRV;

namespace ShaderCompiler;

public static partial class ShaderCompiler
{
    private static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            throw new ArgumentException("invalid argument length");
        }

        var sourceDir = args[0];
        var compileDir = args[1];

        var sourceShaderDir = new DirectoryInfo(sourceDir);
        var compileShaderDir = new DirectoryInfo(compileDir);

        CompileShaders(sourceShaderDir, compileShaderDir);

        Console.WriteLine("Compilation completed");
    }

    [PublicAPI]
    public static void CompileShaders(DirectoryInfo sourceDir, DirectoryInfo compileDir, bool debug = false)
    {
        foreach (var shaderFile in sourceDir.GetFiles("*", SearchOption.AllDirectories))
        {
            var fileExtension = shaderFile.Extension.Substring(1);
            var shaderStage =
                fileExtension switch
                {
                    "vert" => ShaderStages.Vertex,
                    "frag" => ShaderStages.Fragment,
                    "comp" => ShaderStages.Compute,
                    "geom" => ShaderStages.Geometry,
                    "glsl" => ShaderStages.None,
                    _ => throw new InvalidOperationException($"Invalid shader extension: {shaderFile.FullName}")
                };

            if (shaderStage == ShaderStages.None)
                continue;

            var shaderContent = LoadShaderContent(shaderFile, new List<FileInfo>());

            var compileResult =
                SpirvCompilation.CompileGlslToSpirv(shaderContent, "", shaderStage, new GlslCompileOptions(debug));

            var subDir = shaderFile.DirectoryName!.Length + 1 == sourceDir.FullName.Length
                ? string.Empty
                : shaderFile.DirectoryName.Substring(sourceDir.FullName.Length);

            var compiledShaderFolder = $@"{compileDir}\{subDir}\";
            var compiledShaderName =
                $"{compiledShaderFolder}{Path.GetFileNameWithoutExtension(shaderFile.Name)}_{fileExtension}.spv";

            Directory.CreateDirectory(compiledShaderFolder);
            File.WriteAllBytes(compiledShaderName, compileResult.SpirvBytes);
        }
    }
    
    [GeneratedRegex("#include\\s*[\"](.*?)[\"]")]
    private static partial Regex GetIncludeRegex();

    private static string LoadShaderContent(FileInfo shaderFile, List<FileInfo> includeHierarchy)
    {
        //find all includes by the pattern #include "path"
        
        var shaderContent = File.ReadAllText(shaderFile.FullName);

        foreach (Match match in GetIncludeRegex().Matches(shaderContent))
        {
            var includePath = match.Groups[1].Value;
            var includeFile = Path.Combine(shaderFile.DirectoryName!, includePath);
            var includeFileInfo = new FileInfo(includeFile);

            if (!includeFileInfo.Exists)
            {
                throw new FileNotFoundException($"Include file not found: {includeFileInfo.FullName}");
            }

            if (includeHierarchy.Any(x => x.FullName == includeFileInfo.FullName))
            {
                throw new InvalidOperationException($"Circular include detected: {includeFileInfo.FullName}");
            }
            
            var includeContent = LoadShaderContent(includeFileInfo, new List<FileInfo>(includeHierarchy) {shaderFile});
            //replace the include statement with the include content
            shaderContent = shaderContent.Replace(match.Value, includeContent);
        }
        
        return shaderContent;
    }
}