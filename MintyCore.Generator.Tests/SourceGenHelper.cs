using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MintyCore.Generator.Tests;

public static class SourceGenHelper
{
    public static Compilation CreateCompilation(params string[] source)
    {
        var syntaxTrees = source.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        return CreateCompilation(syntaxTrees);
    }

    public static Compilation CreateCompilation(params SyntaxTree[] syntaxTrees)
    {
        return CreateCompilation((MetadataReference[]?)null, syntaxTrees);
    }

    public static Compilation CreateCompilation(MetadataReference? additionalReference, params string[] source)
    {
        var syntaxTrees = source.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        return CreateCompilation(additionalReference, syntaxTrees);
    }

    public static Compilation CreateCompilation(MetadataReference? additionalReference, params SyntaxTree[] syntaxTrees)
    {
        return CreateCompilation(additionalReference is null ? null : new[] {additionalReference}, syntaxTrees);
    }

    public static Compilation CreateCompilation(MetadataReference[]? additionalReference,
        params SyntaxTree[] syntaxTrees)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true,
            nullableContextOptions: NullableContextOptions.Enable);

        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
            options);

        if (additionalReference is not null)
        {
            compilation = compilation.AddReferences(additionalReference);
        }

        return compilation;
    }

    public static MemoryStream Compile(string dllName, params string[] sourceFiles)
    {
        var compilation = CreateCompilation(sourceFiles);
        compilation = compilation.WithAssemblyName(dllName);

        var dllStream = new MemoryStream();
        var emitResult = compilation.Emit(dllStream);

        foreach (var diagnostic in emitResult.Diagnostics)
        {
            Console.WriteLine(diagnostic);
        }

        Assert.True(emitResult.Success);
        
        dllStream.Seek(0, SeekOrigin.Begin);
        return dllStream;
    }

    public static void RunGenerator(IIncrementalGenerator generator, out Compilation? outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics, out SyntaxTree[] generatedTrees, params string[] source)
    {
        var syntaxTrees = source.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();
        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        generatedTrees = outputCompilation.SyntaxTrees.Where(t => !syntaxTrees.Contains(t)).ToArray();
    }

    public static void RunGenerator(ISourceGenerator generator, out Compilation? outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics, out SyntaxTree[] generatedTrees, params string[] source)
    {
        var syntaxTrees = source.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        generatedTrees = outputCompilation.SyntaxTrees.Where(t => !syntaxTrees.Contains(t)).ToArray();
    }

    public static void Analyze(DiagnosticAnalyzer analyzer, out ImmutableArray<Diagnostic> diagnostics,
        params string[] source)
    {
        var syntaxTrees = source.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


        diagnostics = compilation.WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync().Result;
    }
}