using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Silk.NET.Shaderc;

namespace MintyCore.ShaderCompiler;

[PublicAPI]
public static unsafe partial class ShaderCompiler
{
    public static Shaderc Api { get; } = Shaderc.GetApi();

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

    public static CompileOptions* GetDefaultCompileOptions()
    {
        var options = Api.CompileOptionsInitialize();


        Api.CompileOptionsSetIncludeCallbacks(options, new PfnIncludeResolveFn(&IncludeResolver),
            new PfnIncludeResultReleaseFn(&IncludeReleaser), null);
        Api.CompileOptionsSetTargetSpirv(options, SpirvVersion.Shaderc16);
        Api.CompileOptionsSetSourceLanguage(options, SourceLanguage.Glsl);

        return options;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IncludeResult* IncludeResolver(void* arg0, byte* requestedPtr, int i, byte* arg3, UIntPtr arg4)
    {
        var requestedSource = Marshal.PtrToStringAnsi((IntPtr)requestedPtr);
        var requestingSource = Marshal.PtrToStringAnsi((IntPtr)arg3);

        if (requestedSource is null) return null;

        var result = (IncludeResult*)NativeMemory.Alloc((UIntPtr)Marshal.SizeOf<IncludeResult>());

        result->SourceName = requestedPtr;
        result->SourceNameLength = (UIntPtr)requestedSource.Length;

        var directory = Path.GetDirectoryName(requestingSource);

        if (directory is null) return result;
        var includePath = Path.Combine(directory, requestedSource);

        if (!File.Exists(includePath)) return result;
        using var fileStream = File.OpenRead(includePath);
        using var reader = new BinaryReader(fileStream);

        var fileBytes = reader.ReadBytes((int)fileStream.Length);
        var handle = GCHandle.Alloc(fileBytes, GCHandleType.Pinned);
        result->Content = (byte*)handle.AddrOfPinnedObject();
        result->ContentLength = (UIntPtr)fileBytes.Length;
        result->UserData = GCHandle.ToIntPtr(handle).ToPointer();

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void IncludeReleaser(void* arg0, IncludeResult* arg1)
    {
        var handle = GCHandle.FromIntPtr((IntPtr)arg1->UserData);
        handle.Free();

        NativeMemory.Free(arg1);
    }

    public static void FreeCompileOptions(CompileOptions* options)
    {
        Api.CompileOptionsRelease(options);
    }

    public static void CompileShader(FileInfo source, FileInfo target, Compiler* compiler,
        CompileOptions* options, string entryPoint, ShaderKind shaderKind)
    {
        if (!source.Exists) throw new FileNotFoundException("Source file not found", source.FullName);

        //alloc unmanaged memory and create a stream reader which reads the source file into the unmanaged memory
        using var sourceStream = new FileStream(source.FullName, FileMode.Open, FileAccess.Read);
        var sourceText = new Span<byte>(NativeMemory.Alloc((UIntPtr)sourceStream.Length), (int)sourceStream.Length);

        var readBytes = sourceStream.Read(sourceText);
        if (readBytes != sourceStream.Length)
        {
            throw new InvalidOperationException("Failed to read the entire file");
        }

        var inputFileName = Marshal.StringToHGlobalAnsi(source.FullName);
        var entryPointName = Marshal.StringToHGlobalAnsi(entryPoint);


        var result = Api.CompileIntoSpv(compiler, (byte*)Unsafe.AsPointer(ref sourceText.GetPinnableReference()),
            (UIntPtr)sourceText.Length, shaderKind, (byte*)inputFileName, (byte*)entryPointName, options);

        var compilationStatus = Api.ResultGetCompilationStatus(result);
        if (compilationStatus != CompilationStatus.Success)
        {
            throw new Exception(
                $"Failed to compile shader: {compilationStatus.ToString()}\n {Api.ResultGetErrorMessageS(result)}");
        }

        var spirvBytes = Api.ResultGetBytes(result);
        var length = Api.ResultGetLength(result);
        var spirvSpan = new ReadOnlySpan<byte>(spirvBytes, (int)length);

        using var fileStream = target.Open(FileMode.Create, FileAccess.Write, FileShare.None);
        fileStream.Write(spirvSpan);

        Api.ResultRelease(result);

        Marshal.FreeHGlobal(inputFileName);
        Marshal.FreeHGlobal(entryPointName);
    }

    [PublicAPI]
    public static void CompileShaders(DirectoryInfo sourceDir, DirectoryInfo compileDir, CompileOptions* options = null)
    {
        if (!sourceDir.Exists) return;
        if (!compileDir.Exists) compileDir.Create();

        var compiler = Api.CompilerInitialize();
        var releaseOptions = options is null;
        if (options is null)
        {
            options = GetDefaultCompileOptions();
        }

        foreach (var shaderFile in sourceDir.GetFiles("*", SearchOption.TopDirectoryOnly))
        {
            var fileExtension = shaderFile.Extension.Substring(1);
            ShaderKind? shaderStage =
                fileExtension switch
                {
                    "vert" => ShaderKind.VertexShader,
                    "frag" => ShaderKind.FragmentShader,
                    "comp" => ShaderKind.ComputeShader,
                    "geom" => ShaderKind.GeometryShader,
                    "glsl" => null,
                    _ => throw new InvalidOperationException($"Invalid shader extension: {shaderFile.FullName}")
                };

            if (shaderStage is null)
                continue;


            var compiledShaderFile = new FileInfo(
                $"{Path.Combine(compileDir.FullName, Path.GetFileNameWithoutExtension(shaderFile.Name))}_{fileExtension}.spv");

            CompileShader(shaderFile, compiledShaderFile, compiler, options, "main", shaderStage.Value);
        }

        if (releaseOptions)
        {
            FreeCompileOptions(options);
        }

        Api.CompilerRelease(compiler);
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

            var includeContent =
                LoadShaderContent(includeFileInfo, new List<FileInfo>(includeHierarchy) { shaderFile });
            //replace the include statement with the include content
            shaderContent = shaderContent.Replace(match.Value, includeContent);
        }

        return shaderContent;
    }
}