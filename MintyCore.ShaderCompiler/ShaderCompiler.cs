using System;
using System.IO;
using JetBrains.Annotations;
using Veldrid;
using Veldrid.SPIRV;

namespace ShaderCompiler;

public static class ShaderCompiler
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
                    _ => throw new InvalidOperationException($"Invalid shader extension: {shaderFile.FullName}")
                };

            var shaderContent = File.ReadAllText(shaderFile.FullName);
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
}